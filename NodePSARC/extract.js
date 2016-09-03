const zlib = require('zlib');
const fs = require('fs');
const crypto = require('crypto');
const BufferReader = require('buffer-reader');
const Writer = require('buffer-write');
const mkdirp = require('mkdirp');

var make64 = new Buffer([0x00, 0x00, 0x00]);

var pakFile;

pakFile = 'NMSARC.2BB4E68C.pak';
pakFile = 'NMSARC.9C10B65D.pak';
pakFile = 'NMSARC.B9700502.pak';
pakFile = 'NMSARC.1A1B4C22.pak';
pakFile = 'test.pak';

var data = fs.readFileSync(pakFile);

var br = new BufferReader(data);

var psHeader = {};

psHeader.magic = br.nextBuffer(0x4).toString();
psHeader.version = {
  major: br.nextUInt16BE(),
  minor: br.nextUInt16BE()
};
psHeader.compressionType = br.nextBuffer(0x4).toString();
psHeader.tocLength = br.nextUInt32BE();
psHeader.tocEntrySize = br.nextUInt32BE();
psHeader.tocEntries = br.nextUInt32BE();
psHeader.blockSize = br.nextUInt32BE();
psHeader.archiveFlags = br.nextUInt32BE();

psHeader.dataType = Math.ceil(Math.log(psHeader.blockSize) / Math.log(256));

console.log(psHeader);

var TOCs = [];
var tmp;
for (var i = 0; i < psHeader.tocEntries; i++) {
  tmp = {};
  tmp.MD5 = br.nextBuffer(0x10).toString('hex');
  tmp.blockIndex = br.nextUInt32BE();
  tmp.dataLength = br.nextBuffer(0x5).readUIntBE(0, 5);
  tmp.offset = br.nextBuffer(0x5).readUIntBE(0, 5);

  TOCs.push(tmp);
}
//console.log(TOCs);
var BlockSizes = [];
//for (var i = 0; i < psHeader.tocEntries; i++) {
while (br.tell() < psHeader.tocLength) {
  switch (psHeader.dataType) {
    case 2:
      BlockSizes.push(br.nextUInt16BE());
      break;
    case 3:
      BlockSizes.push(br.nextBuffer(0x3).readUIntBE(0, 3));
      break;
    case 4:
      BlockSizes.push(br.nextUInt32BE());
      break;
    case 5:
      BlockSizes.push(br.nextBuffer(0x5).readUIntBE(0, 3));
      break;
    case 6:
      BlockSizes.push(br.nextUInt64BE());
      break;
  }
}
//console.log(BlockSizes);

var rawCount = 0;
var prevBlockSizes = 0;
var curBlock = TOCs[0].blockIndex;
var bw = new Writer();
var deflatedData;
while (bw.length < TOCs[0].dataLength) {
  console.log(curBlock);
  prevBlockSizes = prevBlockSize(rawCount, psHeader.blockSize, BlockSizes, TOCs[0].blockIndex, curBlock);
  curBlockSize = BlockSizes[curBlock];
  if (curBlock > BlockSizes.length - 1) break;
  curBlock += 1;
  deflatedData = data.slice(TOCs[0].offset + prevBlockSizes, TOCs[0].offset + prevBlockSizes + curBlockSize);
  if (deflatedData.slice(0, 2).toString() == new Buffer([0x78, 0xda]).toString()) {
    bw.write(zlib.inflateSync(deflatedData));
  } else {
    bw.write(deflatedData);
  }
}
var Manifest = bw.toBuffer().toString().split("\n");
TOCs.shift(); // shift off the manifest

if (Manifest.length != TOCs.length) throw 'Manifest and TOC Length mismatch';

var extractedTmp;
var curTOC;
var curBlockSize;
var curFilename;
var curPath;
var remainingLength;
for (var i = 0; i < TOCs.length; i++) {
  //console.log('====================================================');
  curTOC = TOCs[i];
  curFilename = Manifest[i];
  curPath = curFilename.split('/');
  curPath.pop()
  mkdirp.sync(curPath.join('/'));
  curBlock = curTOC.blockIndex;

  console.log(i, curFilename);
  // console.log(curTOC.MD5);
  // console.log(crypto.createHash('md5').update(curFilename.toUpperCase()).digest("hex"));
  // console.log('----------------------------------------------------');

  extractedTmp = '';
  bw = new Writer();
  rawCount = 0;

  while (bw.length < curTOC.dataLength) {
    if (curBlock > BlockSizes.length - 1) break;
    // console.log('RemainingLength:', curTOC.dataLength - extractedTmp.length);
    // console.log('BlockSize:', curBlockSize);
    //console.log('offset:', psHeader.tocLength + prevBlockSizes);
    curBlockSize = BlockSizes[curBlock];
    if (curBlockSize == 0) {
      prevBlockSizes = prevBlockSize(rawCount, psHeader.blockSize, BlockSizes, curTOC.blockIndex, curBlock);
      //console.log('--add raw data', curTOC.dataLength);
      bw.write(data, curTOC.offset + prevBlockSizes, curTOC.offset + prevBlockSizes + psHeader.blockSize);
      rawCount += 1;
      //die();
    } else {
      prevBlockSizes = prevBlockSize(rawCount, psHeader.blockSize, BlockSizes, curTOC.blockIndex, curBlock);
      deflatedData = data.slice(curTOC.offset + prevBlockSizes, curTOC.offset + prevBlockSizes + curBlockSize);
      //console.log('Cur Block Data: ', deflatedData);

      if (deflatedData.slice(0, 2).readUInt16BE() == new Buffer([0x78, 0xda]).readUInt16BE()) {
        //console.log('+++add inflated data');
        bw.write(zlib.inflateSync(deflatedData));
      } else {
        //console.log('--add deflated data', curTOC.dataLength);
        bw.write(deflatedData);
      }
    }
    curBlock += 1;
  }
  //console.log('dataLength Reached:', bw.length == curTOC.dataLength);
  if (bw.length != curTOC.dataLength) {
    console.log(' ! ! ! Length Mis Match', bw.length, curTOC.dataLength);
    throw '! ! ! Length Mis Match';
  }
  // console.log(bw.length, curTOC.dataLength);
  fs.writeFileSync(curFilename, bw.toBuffer());
}

function prevBlockSize(rawCount, blockSize, BlockSizes, startBlock, curBlock) {
  var prevBlockSizes = 0;
  for (var i = startBlock; i < curBlock; i++) {
    prevBlockSizes += BlockSizes[i];
    if (BlockSizes[i] == 0) {
      //console.log('! ! ! block', i, 'is zero ! ! !');
    }
  }
  return prevBlockSizes + (rawCount * blockSize);
}
