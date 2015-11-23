﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Example.EclProvider.Api;
using Example.EclProvider.Extensions;
using Tridion.ExternalContentLibrary.V2;

namespace Example.EclProvider
{
    /// <summary>
    /// Represents a Flickr Photo with all details loaded. 
    /// </summary>
    public class FlickrPhoto : ListItem, IContentLibraryMultimediaItem
    {
        private const string NamespaceUri = "http://flickr.com/services/api";
        private const string RootElementName = "Metadata";

        public FlickrPhoto(int publicationId, FlickrInfo info) : base(publicationId, info)
        {
            // if info needs to be fully loaded, do so here
        }

        public string Filename
        {
            get
            {
                string url = Flickr.GetPhotoUrl(Info, PhotoSizeEnum.Medium);
                return url.Substring(url.LastIndexOf('/') + 1);
            }
        }

        public IContentResult GetContent(IList<ITemplateAttribute> attributes)
        {
            // determine width from attributes
            string width = attributes.Attribute("width");
            if (String.IsNullOrEmpty(width))
            {
                string style = attributes.Attribute("style");
                if (!String.IsNullOrEmpty(style) && style.Contains("width"))
                {
                    // style contains something like "width: 320px; height: 240px;"
                    width = style.Split(';').Select(t => t.Trim()).First(t => t.StartsWith("width")).Split(':')[1].Replace("px", String.Empty);
                }
            }
            if (String.IsNullOrEmpty(width))
            {
                width = Width.ToString();
            }

            // because we want SDL Tridion to publish the item, we will return the content stream for this Flickr photo
            using (WebClient webClient = new WebClient())
            {
                // Flickr photo url adjusted for the requested size
                string url = Flickr.GetPhotoUrl(Info, Convert.ToInt32(width));

                ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
                
                using (Stream stream = new MemoryStream(webClient.DownloadData(url)))
                {
                    return Provider.HostServices.CreateContentResult(stream, stream.Length, MimeType);
                }
            }
        }

        public string GetDirectLinkToPublished(IList<ITemplateAttribute> attributes)
        {
            return null;
        }

        public string GetTemplateFragment(IList<ITemplateAttribute> attributes)
        {
            return null;
        }

        public int? Height
        {
            get { return Flickr.MaxHeight; }
        }

        public string MimeType
        {
            get { return "image/jpeg"; }
        }

        public int? Width
        {
            get { return Flickr.MaxWidth; }
        }

        public bool CanGetViewItemUrl
        {
            get { return true; }
        }

        public bool CanUpdateMetadataXml
        {
            get { return false; }
        }

        public bool CanUpdateTitle
        {
            get { return false; }
        }

        public DateTime? Created
        {
            get { return Info.Created; }
        }

        public string CreatedBy
        {
            get { return Provider.Flickr.UserId; }
        }

        public string MetadataXml
        {
            get { return string.Format("<{0} xmlns=\"{1}\"><Description>{2}</Description><Width>{3}</Width><Height>{4}</Height><Filename>{5}</Filename><MimeType>{6}</MimeType></{0}>", RootElementName, NamespaceUri, Info.Description, Width, Height, Filename, MimeType); }
            set { throw new NotSupportedException(); }
        }

        public ISchemaDefinition MetadataXmlSchema
        {
            get
            {
                ISchemaDefinition schema = Provider.HostServices.CreateSchemaDefinition(RootElementName, NamespaceUri);
                schema.Fields.Add(Provider.HostServices.CreateMultiLineTextFieldDefinition("Description", "Description", 0, 1, 7));
                schema.Fields.Add(Provider.HostServices.CreateNumberFieldDefinition("Width", "Width", 0, 1));
                schema.Fields.Add(Provider.HostServices.CreateNumberFieldDefinition("Height", "Height", 0, 1));
                schema.Fields.Add(Provider.HostServices.CreateSingleLineTextFieldDefinition("Filename", "Filename", 0, 1));
                schema.Fields.Add(Provider.HostServices.CreateSingleLineTextFieldDefinition("MimeType", "MIME type", 0, 1));
                return schema;
            }
        }

        public string ModifiedBy
        {
            get { return CreatedBy; }
        }

        public IEclUri ParentId
        {
            get
            {
                // return folder uri (Flickr photoset)
                return Provider.HostServices.CreateEclUri(
                    Id.PublicationId,
                    Id.MountPointId,
                    Info.PhotoSetId,
                    "set",
                    EclItemTypes.Folder);
            }
        }

        public IContentLibraryItem Save(bool readback)
        {
            // as saving isn't supported, the result of saving is always the item itself
            return readback ? this : null;
        }
    }
}
