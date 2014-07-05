using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Messaging;
using Autodesk.Max;


namespace MaxSceneServer
{
    public partial class MaxSceneServer
    {
        IEnumerable<MaterialInformation> GetMaterials(MessageMaterialRequest request)
        {
            if (request.m_nodeName != null)
            {
                foreach (var node in GetNode(request.m_nodeName))
                {
                    yield return GetMaterialProperties(node.Mtl, request);
                }
            }
            if (request.m_handle > 0)
            {
                yield return GetMaterialProperties(_gi.Animatable.GetAnimByHandle(new UIntPtr(request.m_handle)) as IMtl, request);
            }
        }

        MaterialInformation GetMaterialProperties(IMtl material, MessageMaterialRequest request)
        {
            if (material == null)
            {
                return null;
            }

            if (request.m_materialIndex < 0)
            {
                return GetMaterialProperties(material);
            }

            return GetMaterialProperties(material.GetSubMtl(request.m_materialIndex));
        }

        MaterialInformation GetMaterialProperties(IMtl material)
        {
            if (material == null)
            {
                return null;
            }

            MaterialInformation m = new MaterialInformation();
            
            m.m_className = material.ClassName;
            m.m_materialName = material.Name;
            m.m_handle = _gi.Animatable.GetHandleByAnim(material).ToUInt64();

            var prps = EnumerateProperties(material).ToList();

            foreach (var p in EnumerateProperties(material))
            {
                m.MaterialProperties.Add(new MaterialProperty(p.m_parameterName, p.m_internalName, p.GetValue()));
            }

            return m;
        }

        MessageMapContent GetMap(MessageMapRequest request)
        {
            Parameter mapParam = new Parameter(request.m_map.m_parameterReference);

            //if (mapParam.IsBitmapType) //bitmaps we can work out how to handle later
            //{
            //    return GetBitmap(mapParam, request);
            //}
            if (mapParam.IsTexmapType)
            {
                return GetTexamp(mapParam, request);
            }

            return new MessageMapContent();
        }

        /* From maxsdk\include\bitmap.h */

        protected const int BMM_NO_TYPE = 0;   //!<  Not allocated yet
        protected const int BMM_LINE_ART = 1;  //!<  1-bit monochrome image
        protected const int BMM_PALETTED = 2;  //!<  8-bit paletted image. Each pixel value is an index into the color table.
        protected const int BMM_GRAY_8 = 3;    //!<  8-bit grayscale bitmap.
        protected const int BMM_GRAY_16 = 4;   //!<  16-bit grayscale bitmap.
        protected const int BMM_TRUE_16 = 5;   //!<  16-bit true color image.
        protected const int BMM_TRUE_32 = 6;   //!<  32-bit color: 8 bits each for Red, Green, Blue, and Alpha.
        protected const int BMM_TRUE_64 = 7;   //!<  64-bit color: 16 bits each for Red, Green, Blue, and Alpha.
        /*! This format uses a logarithmic encoding of luminance and U' and V' in the CIE perceptively uniform
        space. It spans 38 orders of magnitude from 5.43571 to 1.84467 in steps of about 0.3% luminance steps. It
        includes both positive and negative colors. A separate 16 bit channel is kept for alpha values. */
        protected const int BMM_LOGLUV_32 = 13;
        /*! This format is similar to \ref BMM_LOGLUV_32 except is uses smaller values to give a span of 5 order of
        magnitude from 1/4096 to 16 in 1.1% luminance steps. A separate 8 bit channel is kept for alpha values. */
        protected const int BMM_LOGLUV_24 = 14;
        /*! This format is similar to \ref BMM_LOGUV_24, except the 8 bit alpha value is kept with the 24 bit 
        color value in a single 32 bit word. */
        protected const int BMM_LOGLUV_24A = 15;
        protected const int BMM_REALPIX_32 = 16;   //!<  The "Real Pixel" format.
        protected const int BMM_FLOAT_RGBA_32 = 17;   //!<  32-bit floating-point per component (non-compressed), RGB with or without alpha
        protected const int BMM_FLOAT_GRAY_32 = 18;   //!<  32-bit floating-point (non-compressed), monochrome/grayscale

        //-- Information Only

        protected const int BMM_TRUE_24 = 8;    //!< 24-bit color: 8 bits each for Red, Green, and Blue. Cannot be written to.
        protected const int BMM_TRUE_48 = 9;    //!< 48-bit color: 16 bits each for Red, Green, and Blue. Cannot be written to.
        protected const int BMM_YUV_422 = 10;   //!< This is the YUV format - CCIR 601. Cannot be written to.
        protected const int BMM_BMP_4 = 11;  //!< Windows BMP 16-bit color bitmap.  Cannot be written to.
        protected const int BMM_PAD_24 = 12;   //!< Padded 24-bit (in a 32 bit register).  Cannot be written to.
        /*! ONLY returned by the GetStoragePtr() method of BMM_FLOAT_RGBA_32 storage, NOT an actual storage type!
        When GetStoragePtr() returns this type, the data should be interpreted as three floating-point values,
        corresponding to Red, Green, and Blue (in this order). */
        protected const int BMM_FLOAT_RGB_32 = 19;
        /*! ONLY returned by the GetAlphaStoragePtr() method of BMM_FLOAT_RGBA_32 or BMM_FLOAT_GRAY_32 storage,
        NOT an actual storage type! When GetStorageAlphaPtr() returns this type, the data should be interpreted
        as floating-point values one value per pixel, corresponding to Alpha. */
        protected const int BMM_FLOAT_A_32 = 20;

        protected const int MAP_HAS_ALPHA = (1 << 1);//!< The bitmap has an alpha channel.

        MessageMapContent GetTexamp(Parameter mapParam, MessageMapRequest request)
        {
            var texmap = mapParam.GetTexmap();

            //http://docs.autodesk.com/3DSMAX/16/ENU/3ds-Max-SDK-Programmer-Guide/index.html?url=files/GUID-FD9764C9-EE84-4A1A-BC62-87AE6AF86CC1.htm,topicNumber=d30e31073
            //http://docs.autodesk.com/3DSMAX/16/ENU/3ds-Max-SDK-Programmer-Guide/index.html?url=files/GUID-FD9764C9-EE84-4A1A-BC62-87AE6AF86CC1.htm,topicNumber=d30e31073

            IBitmapInfo bmpInfo = _gi.BitmapInfo.Create();

            bmpInfo.SetType(BMM_TRUE_32);
            bmpInfo.SetWidth(request.m_width);
            bmpInfo.SetHeight(request.m_height);
            bmpInfo.SetFlags(MAP_HAS_ALPHA);
            bmpInfo.SetCustomFlag(0);
            bmpInfo.SetFirstFrame(0);
            bmpInfo.SetLastFrame(0);

            IBitmap bmp = _gi.CreateBitmapFromBitmapInfo(bmpInfo);

            texmap.RenderBitmap(0, bmp, 1.0f, request.m_filter);

            Directory.CreateDirectory(Path.GetDirectoryName(request.m_filename));

            //The bmpInfo contains the filename - note it doesnt have to be the same bmpInfo as created above thats just the easiest way to do it here
            bmpInfo.SetName(request.m_filename);
            bmp.OpenOutput(bmpInfo);
            bmp.Write(bmpInfo, 0);
            bmp.Close(bmpInfo, 0);

            //Max prepends the filename with the sequence number, so rename afterwards
            string extension = System.IO.Path.GetExtension(request.m_filename);
            string maxfilename = request.m_filename.Substring(0, request.m_filename.Length - extension.Length) + "0000" + extension;

            if (File.Exists(request.m_filename))
            {
                File.Delete(request.m_filename);
            }
            System.IO.File.Move(maxfilename, request.m_filename);


            return new MessageMapFilename(request.m_filename);
        }
    }
}
