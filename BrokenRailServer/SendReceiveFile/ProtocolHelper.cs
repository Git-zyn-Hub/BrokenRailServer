using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace BrokenRailServer.SendReceiveFile
{
    public class ProtocolHelper
    {
        private XmlNode fileNode;
        private XmlNode root;

        public ProtocolHelper(string protocol)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(protocol);
            root = doc.DocumentElement;
            fileNode = root.SelectSingleNode("file");
        }

        //此时的protocol已定位单条完整的protocol  
        private FileRequestMode GetFileMode()
        {
            string mode = fileNode.Attributes["mode"].Value;
            mode = mode.ToLower();
            if (mode == "Upload")
            {
                return FileRequestMode.Upload;
            }
            else
            {
                return FileRequestMode.Download;
            }
        }

        //获取单条协议包含的信息  
        public FileProtocol GetProtocol()
        {
            FileRequestMode mode = GetFileMode();
            string fileName = "";
            int port = 0;
            fileName = fileNode.Attributes["name"].Value;
            port = Convert.ToInt32(fileNode.Attributes["port"].Value);

            return new FileProtocol(mode, port, fileName);
        }
    }
}
