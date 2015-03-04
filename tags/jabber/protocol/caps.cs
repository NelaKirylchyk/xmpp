// --------------------------------------------------------------------------------------------------------------------
// <copyright company="" file="caps.cs">
//   
// </copyright>
// <summary>
//   The namespace.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using System.Xml.Linq;
using XMPP.Registries;

namespace XMPP.Tags.Jabber.Protocol.Caps
{
    public class Namespace
    {
        public const string Name = "http://jabber.org/protocol/caps";

        public static readonly XName C = XName.Get("c", Name);
    }

    [XmppTag(typeof(Namespace), typeof(C))]
    public class C : Tag
    {
        public C() : base(Namespace.C)
        {
        }

        public C(XElement other) : base(other)
        {
        }

        public string Ext
        {
            get { return (string)GetAttributeValue("ext"); }
            set { SetAttributeValue("ext", value); }
        }

        public string Hash
        {
            get { return (string)GetAttributeValue("hash"); }
            set { SetAttributeValue("hash", value); }
        }

        public string Node
        {
            get { return (string)GetAttributeValue("node"); }
            set { SetAttributeValue("node", value); }
        }

        public string Ver
        {
            get { return (string)GetAttributeValue("ver"); }
            set { SetAttributeValue("ver", value); }
        }
    }
}

/*
<?xml version='1.0' encoding='UTF-8'?>

<xs:schema
    xmlns:xs='http://www.w3.org/2001/XMLSchema'
    targetNamespace='http://jabber.org/protocol/caps'
    xmlns='http://jabber.org/protocol/caps'
    elementFormDefault='qualified'>

  <xs:annotation>
    <xs:documentation>
      The protocol documented by this schema is defined in
      XEP-0115: http://www.xmpp.org/extensions/xep-0115.html
    </xs:documentation>
  </xs:annotation>

  <xs:element name='c'>
    <xs:complexType>
      <xs:simpleContent>
        <xs:extension base='empty'>
          <xs:attribute name='ext' type='xs:NMTOKENS' use='optional'/>
          <xs:attribute name='hash' type='xs:NMTOKEN' use='required'/>
          <xs:attribute name='node' type='xs:string' use='required'/>
          <xs:attribute name='ver' type='xs:string' use='required'/>
        </xs:extension>
      </xs:simpleContent>
    </xs:complexType>
  </xs:element>

  <xs:simpleType name='empty'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value=''/>
    </xs:restriction>
  </xs:simpleType>

</xs:schema>

*/