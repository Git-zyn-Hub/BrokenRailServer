using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Media;
using System.Xml.Linq;

namespace BrokenRailServer.Classes
{
    public class XmlHelper
    {
        private string _xmlPath;

        public string XmlPath
        {
            get
            {
                return _xmlPath;
            }

            set
            {
                _xmlPath = value;
            }
        }

        public void XmlInitial()
        {
            bool curveConfigExists = CheckCurveConfigExists();
            if (!curveConfigExists)
            {
                File.Create(XmlPath).Close();
            }
            else
            {
                return;
            }
            XDocument doc = new XDocument
            (
                new XDeclaration("1.0", "utf-8", "yes"),
                new XElement
                (
                    "Remember",
                    new XElement
                    (
                        "ConfigInitialInfoPassword", "jtsy2016jtsy"
                    )
                )
            );

            // 保存为XML文件
            doc.Save(XmlPath);
        }

        private bool CheckCurveConfigExists()
        {
            return File.Exists(XmlPath);
        }


        public void ModifyXmlConfigInitialInfoPassword(string pwd)
        {
            XDocument xd = XDocument.Load(XmlPath);
            ///查询修改的元素  
            XElement root = xd.Root;
            ///修改元素  
            if (root != null)
            {
                ///设置新的属性  
                root.SetElementValue("ConfigInitialInfoPassword", pwd);
            }
            xd.Save(XmlPath);
        }

        public string GetXmlElementValue(string elementName)
        {
            XDocument xd = XDocument.Load(XmlPath);
            ///查询修改的元素  
            XElement element = xd.Root.Element(elementName);
            if (element != null)
            {
                return element.Value.ToString();
            }
            return string.Empty;
        }
    }
}
