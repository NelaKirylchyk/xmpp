// --------------------------------------------------------------------------------------------------------------------
// <copyright company="" file="discoXitems.cs">
//   
// </copyright>
// <summary>
//   The namespace.
// </summary>
// 
// --------------------------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using XMPP.Registries;

namespace XMPP.Tags.Jabber.Protocol.DiscoXitems
{
    public class Namespace
    {
        public const string Name = "http://jabber.org/protocol/disco#items";

        public static readonly XName Query = XName.Get("query", Name);
        public static readonly XName Item = XName.Get("item", Name);
    }

    [XmppTag(typeof(Namespace), typeof(Query))]
    public class Query : Tag
    {
        public Query() : base(Namespace.Query)
        {
        }

        public Query(XElement other) : base(other)
        {
        }

        public string Node
        {
            get { return (string)GetAttributeValue("node"); }
            set { SetAttributeValue("node", value); }
        }

        public IEnumerable<Item> ItemElements
        {
            get { return Elements<Item>(Namespace.Item); }
        }
    }

    [XmppTag(typeof(Namespace), typeof(Item))]
    public class Item : Tag
    {
        public Item() : base(Namespace.Item)
        {
        }

        public Item(XElement other) : base(other)
        {
        }

        public string Jid
        {
            get { return (string) GetAttributeValue("jid"); }
            set
            {
                if (value.Length < 8 || value.Length > 3071)
                {
                    throw new Exception("Text out of range");
                }

                SetAttributeValue("jid", value);
            }
        }

        public string Name
        {
            get { return (string)GetAttributeValue("name"); }
            set { SetAttributeValue("name", value); }
        }

        public string Node
        {
            get { return (string)GetAttributeValue("node"); }
            set { SetAttributeValue("node", value); }
        }
    }
}

/*
<?xml version='1.0' encoding='UTF-8' ?>

<xs:schema
    xmlns:xs='http://www.w3.org/2001/XMLSchema'
    targetNamespace='http://jabber.org/protocol/disco#items'
    xmlns='http://jabber.org/protocol/disco#items'
    elementFormDefault='qualified'>

  <xs:annotation>
    <xs:documentation>
      The protocol documented by this schema is defined in
      XEP-0030: http://www.xmpp.org/extensions/xep-0030.html
    </xs:documentation>
  </xs:annotation>

  <xs:element name='query'>
    <xs:complexType>
      <xs:sequence minOccurs='0'>
        <xs:element ref='item' minOccurs='0' maxOccurs='unbounded'/>
      </xs:sequence>
      <xs:attribute name='node' type='xs:string' use='optional'/>
    </xs:complexType>
  </xs:element>

  <xs:element name='item'>
    <xs:complexType>
      <xs:simpleContent>
        <xs:extension base='empty'>
          <xs:attribute name='jid' type='fullJIDType' use='required'/>
          <xs:attribute name='name' type='xs:string' use='optional'/>
          <xs:attribute name='node' type='xs:string' use='optional'/>
        </xs:extension>
      </xs:simpleContent>
    </xs:complexType>
  </xs:element>

  <xs:simpleType name='fullJIDType'>
    <xs:restriction base='xs:string'>
      <xs:minLength value='8'/>
      <xs:maxLength value='3071'/>
    </xs:restriction>
  </xs:simpleType>

  <xs:simpleType name='empty'>
    <xs:restriction base='xs:string'>
      <xs:enumeration value=''/>
    </xs:restriction>
  </xs:simpleType>

</xs:schema>

*/