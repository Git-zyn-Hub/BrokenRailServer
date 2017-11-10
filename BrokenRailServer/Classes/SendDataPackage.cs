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
        //private byte _length;
        //private byte _sourceAddress;
        //private byte _destinationAddress;
        //private byte _dataType;
        //private byte[] _dataContent;
        private byte _checksum = 0;
        public SendDataPackage()
        {

        }
        public byte[] PackageSendData(byte sourceAddr, byte destinationAddr, byte dataType, byte[] dataContent)
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
    }
}
