const zlib = require('zlib');
const fs = require('fs');
const crypto = require('crypto');
const BufferReader = require('buffer-reader');
const Writer = require('buffer-write');
const glob = require('glob');

const COMPRESSION_TYPE = 'zlib';
const ENTRY_SIZE = 30;
const BLOCK_SIZE = 65536; // 16777216 or 4294967296
const ARCHIVE_FLAGS = 1; // Ignore Case

var outputFile = 'WRITESTREAM.TEST.pak';

console.log('Getting GlobList');
var pattern
pattern = 'PIPELINES/**';
pattern = 'MODELS/**';
pattern = '?(AUDIO)?(MUSIC)/**';
pattern = 'TEXTURES/**';
pattern = 'TEXTURES/**';
pattern = 'test.txt';
var globList = getFileList(pattern, {
  nodir: true
}).sort();
var fileSizes = [];

console.log('Converting Glob to Object');
var filesObj = getFilesObject(globList);

console.log('Sort file listing');
var fileList = [];
getDirListing(fileList, filesObj, []);

console.log('Creating Manifest');
var Manifest = fileList.join('\n');

console.log('Getting uncompressed file sizes');
for (var i = 0; i < fileList.length; i++) {
  fileSizes.push(fs.lstatSync(fileList[i]).size);
}

console.log('Loading file data and matching identical files');
var fileData = [];
var fileMD5s = [];
var fileMatches = [];
var tmpFileData;
var tmpMD5;
for (var f = 0; f < fileList.length; f++) {
  tmpFileData = fs.readFileSync(fileList[f]);
  tmpMD5 = crypto.createHash('md5').update(tmpFileData).digest("hex");
  fileMatches.push(fileMD5s.indexOf(tmpMD5));
  fileMD5s.push(tmpMD5);
  fileData.push(tmpFileData);
}

console.log('Generating file block data');
var fileBlockCount = [];
var totalBlocks = 0;
fileBlockCount.push(Math.ceil(Manifest.length / BLOCK_SIZE));
totalBlocks += fileBlockCount[0];
for (var i = 0; i < fileSizes.length; i++) {
  if (fileMatches[i] == -1) {
      var blockCount = Math.ceil(fileSizes[i] / BLOCK_SIZE);
    fileBlockCount.push((blockCount>0) ? blockCount : 1);
    totalBlocks += fileBlockCount[i + 1]; // + 1 offset for manifest
  } else {
    fileBlockCount.push(0);
  }
}

console.log('Generatile file block offsets');
var curBlockCount = 0;
var fileBlockStart = [];
for (var i = 0; i < fileBlockCount.length; i++) {
  if (i > 0) {
    if (fileMatches[i - 1] == -1) {
      fileBlockStart.push(curBlockCount);
      curBlockCount += fileBlockCount[i];
    } else {
      fileBlockStart.push(fileBlockStart[fileMatches[i] + 1]);
    }
  } else {
    fileBlockStart.push(curBlockCount);
    curBlockCount += fileBlockCount[i];
  }
}

console.log('Creating Manifest block');
var blocks = [];
var br;
br = new BufferReader(new Buffer(Manifest));
for (var i = 0; i < fileBlockCount[0]; i++) {
  if (fileMatches[i] == -1) {
    bw = new Writer();
    if (Manifest.length - br.tell() > BLOCK_SIZE) {
      bw.write(br.nextBuffer(BLOCK_SIZE));
    } else {
      bw.write(br.nextBuffer(Manifest.length - br.tell()));
    }
    //console.log(blocks.length, bw.length);
    blocks.push(bw.toBuffer());
  }
}

console.log('Creating Data Blocks');
for (var f = 0; f < fileList.length; f++) {
  if (fileMatches[f] == -1) {
    br = new BufferReader(fileData[f]);
    for (var b = 0; b < fileBlockCount[f + 1]; b++) {
      bw = new Writer();
      if (fileSizes[f] - br.tell() > BLOCK_SIZE) {
        bw.write(br.nextBuffer(BLOCK_SIZE));
      } else {
        bw.write(br.nextBuffer(fileSizes[f] - br.tell()));
      }
      //console.log(blocks.length, bw.length);
      blocks.push(bw.toBuffer());
    }
  }
}

console.log('Try compressing data and get final block size');
var blockSizes = [];
for (var i = 0; i < blocks.length; i++) {
  blocks[i] = doZLib(blocks[i]);
  blockSizes.push(blocks[i].length);
}

console.log('Start writing PSARC file');

console.log('Write PSARC Header');
var tocLength = 32 + ((fileList.length + 1) * ENTRY_SIZE) + (totalBlocks * 2);
//console.log('tocLength', tocLength);
var writeStream = fs.createWriteStream(outputFile, {
  autoClose: false
});

var bw = new Writer();
bw.write('PSAR', 4);
bw.writeUInt16BE(1);
bw.writeUInt16BE(4);
bw.write(COMPRESSION_TYPE, 4);
bw.writeUInt32BE(tocLength);
bw.writeUInt32BE(ENTRY_SIZE);
bw.writeUInt32BE(fileList.length + 1);
bw.writeUInt32BE(BLOCK_SIZE);
bw.writeUInt32BE(ARCHIVE_FLAGS);
//console.log(bw.toBuffer());

console.log('Write Manifest entry');
// Write Manifest Data
bw.fill(0, 16); // MD5
bw.writeUInt32BE(0); // blockIndex
bw.write(getUInt40BE(Manifest.length)); // dataLength
bw.write(getUInt40BE(tocLength)); // offset

// Write current bw to file
writeStream.write(bw.toBuffer());
bw = Writer();

console.log('Write file entries');
for (var i = 0; i < fileList.length; i++) {
  console.log('writing', fileList[i], crypto.createHash('md5').update(fileList[i].toUpperCase()).digest("hex"));
  bw.write(crypto.createHash('md5').update(fileList[i].toUpperCase()).digest()); // MD5
  if (fileMatches[i] == -1) {
    bw.writeUInt32BE(fileBlockStart[i + 1]); // blockIndex
  } else {
    bw.writeUInt32BE(fileBlockStart[fileMatches[i] + 1]); // blockIndex
  }
  bw.write(getUInt40BE(fileSizes[i])); // dataLength
  if (fileMatches[i] == -1) {
    bw.write(getUInt40BE(tocLength + getBlockOffset(blockSizes, fileBlockStart[i + 1]))); // offset
  } else {
    bw.write(getUInt40BE(tocLength + getBlockOffset(blockSizes, fileBlockStart[fileMatches[i] + 1]))); // offset
  }
}

// Write current bw to file
writeStream.write(bw.toBuffer());
bw = Writer();

console.log('Write data block sizes');
for (var i = 0; i < blocks.length; i++) {
  if (blockSizes[i] < BLOCK_SIZE) {
    switch (Math.ceil(Math.log(BLOCK_SIZE) / Math.log(256))) {
      case 2:
        bw.writeUInt16BE(blockSizes[i]);
        break;
      case 3:
        bw.write(getUInt24BE(blockSizes[i]));
        break;
      case 4:
        bw.writeUInt32BE(blockSizes[i]);
        break;
      case 5:
        bw.write(getUInt40BE(blockSizes[i]));
        break;
      case 6:
        bw.writeUInt32BE(blockSizes[i]);
        break;
    }
  } else {
    bw.writeUInt16BE(0);
  }
}

// Write current bw to file
writeStream.write(bw.toBuffer());
bw = Writer();

console.log('Write data blocks');
for (var i = 0; i < blocks.length; i++) {
  //bw.write(blocks[i]);
  // Write raw blocks to file
  writeStream.write(blocks[i]);
}
// End write stream and close file
writeStream.end();

function doZLib(data) {
  zlibData = zlib.deflateSync(data, {
    level: 9
  });
  if (data.length > zlibData.length) {
    return zlibData;
  } else {
    return data;
  }
}

function getBlockOffset(blockSizes, end) {
  var offset = 0;
  for (var i = 0; i < end; i++) {
    offset += blockSizes[i];
  }
  return offset;
}

function getUInt40BE(i) {
  var tmp = new Writer();
  tmp.writeUInt64BE(i);
  var bw = new Writer();
  bw.write(tmp.toBuffer(), 3, 8);
  return bw.toBuffer();
}

function getUInt24BE(i) {
  var tmp = new Writer();
  tmp.writeUInt32BE(i);
  var bw = new Writer();
  bw.write(tmp.toBuffer(), 1, 4);
  return bw.toBuffer();
}

function getFileList(dir) {
  var fileList = glob.sync(dir, {
      nodir: true
    })
    // console.log(fileList);
    // throw '';
  return fileList;
}

function getFilesObject(files) {
  var filesObj = [];
  for (var i = 0; i < files.length; i++) {
    var path = files[i].split('/');
    var file = path.pop();
    var obj = filesObj;
    //console.log(files[i]);
    for (var p = 0; p < path.length; p++) {
      if (typeof obj[path[p]] == typeof undefined) obj[path[p]] = [];

      obj = obj[path[p]];
    }
    obj.push(file);
  }
  return filesObj;
}

function getDirListing(listing, filesObj, curPath) {
  var curObj;
  Object.keys(filesObj).forEach(function(key) {
    curObj = filesObj[key];
    if (typeof curObj != typeof {}) {
      if (curPath > 0) {
        listing.push(curPath.join('/') + '/' + curObj);
      } else {
        listing.push(curObj);
      }
    }
  });
  Object.keys(filesObj).forEach(function(key) {
    curObj = filesObj[key];
    if (typeof curObj == typeof {}) {
      curPath.push(key);
      getDirListing(listing, curObj, curPath);
      curPath.pop();
    }
  });
}
