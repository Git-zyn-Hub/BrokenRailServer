﻿using BrokenRailServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BrokenRailMonitorViaWiFi
{
    public class SendDataPackage
    {
        private const byte _frameHeader1 = 0x55;
        private const byte _frameHeader2 = 0xAA;
        private const byte _frameRespondHeader1 = 0x66;
        private const byte _frameRespondHeader2 = 0xCC;
        //private byte _length;
        //private byte _sourceAddress;
        //private byte _destinationAddress;
        //private byte _dataType;
        //private byte[] _dataContent;
        private static byte _checksum = 0;
        private static int _checksumRespond = 0;
        public SendDataPackage()
        {

        }
        public static byte[] PackageSendData(byte sourceAddr, byte destinationAddr, byte dataType, byte[] dataContent)
        {
            byte[] result;
            int length = 0;
            length = 7 + dataContent.Length;
            result = new byte[length];
            result[0] = _frameHeader1;
            result[1] = _frameHeader2;
            result[2] = (byte)length;
            result[3] = sourceAddr;
            result[4] = destinationAddr;
            result[5] = dataType;
            for (int i = 0; i < dataContent.Length; i++)
            {
                result[6 + i] = dataContent[i];
            }
            _checksum = 0;
            for (int i = 0; i < length - 1; i++)
            {
                _checksum += result[i];
            }
            result[length - 1] = _checksum;
            return result;
        }

        public static byte[] PackageRespondData(byte sourceAddr, byte destinationAddr, byte dataType, byte[] dataContent)
        {
            byte[] result;
            int length = 0;
            length = 9 + dataContent.Length;
            result = new byte[length];
            result[0] = _frameRespondHeader1;
            result[1] = _frameRespondHeader2;
            result[2] = (byte)((length & 0xFF00) >> 8);
            result[3] = (byte)(length & 0xFF);
            result[4] = sourceAddr;
            result[5] = destinationAddr;
            result[6] = dataType;
            for (int i = 0; i < dataContent.Length; i++)
            {
                result[7 + i] = dataContent[i];
            }
            _checksumRespond = 0;
            for (int i = 0; i < length - 2; i++)
            {
                _checksumRespond += result[i];
            }
            result[length - 2] = (byte)((_checksumRespond & 0xFF00) >> 8);
            result[length - 1] = (byte)(_checksumRespond & 0xFF);
            return result;
        }

        public static byte[] PackageFileHeader(int length, int checkSum, byte totalPCount)
        {
            byte[] result;
            int len = 12;
            result = new byte[len];
            result[0] = _frameHeader1;
            result[1] = _frameHeader2;
            result[2] = _frameRespondHeader1;
            result[3] = _frameRespondHeader2;
            result[4] = 0x44;
            result[5] = (byte)((length & 0xFF00) >> 8);
            result[6] = (byte)(length & 0xFF);
            result[7] = (byte)((checkSum & 0xFF00) >> 8);
            result[8] = (byte)(checkSum & 0xFF);
            result[9] = totalPCount;
            _checksumRespond = 0;
            for (int i = 0; i < len - 2; i++)
            {
                _checksumRespond += result[i];
            }
            result[len - 2] = (byte)((_checksumRespond & 0xFF00) >> 8);
            result[len - 1] = (byte)(_checksumRespond & 0xFF);
            return result;
        }

        public static byte[] PackageFileBody(FileSendType type, byte packageNo, byte[] fileContent)
        {
            byte[] result;
            int length = 0;
            length = 10 + fileContent.Length;
            result = new byte[length];
            result[0] = _frameHeader1;
            result[1] = _frameHeader2;
            result[2] = _frameRespondHeader1;
            result[3] = _frameRespondHeader2;
            result[4] = (byte)type;
            result[5] = (byte)((length & 0xFF00) >> 8);
            result[6] = (byte)(length & 0xFF);
            result[7] = packageNo;
            for (int i = 0; i < fileContent.Length; i++)
            {
                result[8 + i] = fileContent[i];
            }
            _checksumRespond = 0;
            for (int i = 0; i < length - 2; i++)
            {
                _checksumRespond += result[i];
            }
            result[length - 2] = (byte)((_checksumRespond & 0xFF00) >> 8);
            result[length - 1] = (byte)(_checksumRespond & 0xFF);
            return result;
        }
    }
}
