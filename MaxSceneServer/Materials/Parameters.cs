using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Autodesk.Max;
using Messaging;

namespace MaxSceneServer
{
    public static class MaxExtensions
    {
        public static bool EqualsClassID(this IClass_ID classA, int a, int b)
        {
            return ((classA.PartA == a) && (classA.PartB == b));
        }

        public static fRGBA TofRGBA(this IAColor color)
        {
            return new fRGBA(color.R, color.G, color.B, color.A);
        }

        public static fRGBA TofRGBA(this IColor color)
        {
            return new fRGBA(color.R, color.G, color.B, 1.0f);
        }

        public static float Length(this IAColor color)
        {
            return color.R + color.G + color.B + color.A;
        }

        public static float Length(this IColor color)
        {
            return color.R + color.G + color.B;
        }
    }

    public static class LinqExtensions
    {
        //http://stackoverflow.com/questions/489258/linq-distinct-on-a-particular-property

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector(element)))
                {
                    yield return element;
                }
            }
        }

        public static IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector1, Func<TSource, TKey> keySelector2)
        {
            HashSet<TKey> seenKeys = new HashSet<TKey>();
            foreach (TSource element in source)
            {
                if (seenKeys.Add(keySelector1(element)))
                {
                    yield return element;
                }
                if (seenKeys.Add(keySelector2(element)))
                {
                    yield return element;
                }
            }
        }
    }

    public static class IReferenceMakerExtensions
    {
        public static IEnumerable<IIParamBlock2> GetParamBlocks(this IReferenceMaker obj)
        {
            for (int i = 0; i < obj.NumParamBlocks; i++)
            {
                yield return obj.GetParamBlock(i);
            }
        }

        public static IEnumerable<IReferenceTarget> GetReferenceTargets(this IReferenceMaker obj)
        {
            for (int i = 0; i < obj.NumRefs; i++)
            {
                yield return obj.GetReference(i);
            }
        }
    }

    public partial class MaxSceneServer
    {
        #region Parameter Enumeration

        /* Based on http://forums.cgsociety.org/archive/index.php/t-1041239.html */

        protected IEnumerable<Parameter> EnumerateParamBlocks(IReferenceMaker obj)
        {
            for (int i = 0; i < obj.NumParamBlocks; i++)
            {
                foreach (var p in EnumerateParamBlock(obj.GetParamBlock(i)))
                {
                    yield return p;
                }
            }
        }

        protected IEnumerable<Parameter> EnumerateParamBlock(IIParamBlock2 block)
        {
            for (short i = 0; i < block.NumParams; i++)
            {
                int tableLength = 1;

                string paramTypeName = Enum.GetName(typeof(ParamType2), block.GetParameterType(i));

                if (paramTypeName == null)
                {
                    continue; //if the paramtype is unknown, then just skip it
                }

                if (paramTypeName.Contains("Tab"))    //use subanimatables instead of tabs?
                {
                    tableLength = block.Count(i); //this throws an exception if it is called for an index that is not a table
                }

                for (int t = 0; t < tableLength; t++)
                {
                    yield return new Parameter(block, i, t);
                }
            }

            for (int i = 0; i < block.ParamAliasCount; i++)
            {
                yield return new Parameter(block, block.GetParamAlias(i));
            }
        }

        //check references! eq. to what maxscript uses to find all calsses
        //http://forums.cgsociety.org/archive/index.php/t-1041239.html

        protected IEnumerable<Parameter> EnumerateReferences(IReferenceMaker obj, bool recursive = false)
        {
            for (int i = 0; i < obj.NumRefs; i++)
            {
                IReferenceTarget iref = obj.GetReference(i);

                if (iref is IIParamBlock)
                {
                    //Until we can identify IParamBlock member names there is no point in adding them to the list
                    //foreach (var v in EnumerateParamBlock((IIParamBlock)iref))
                    //{
                    //    yield return v;
                    //}
                    continue;
                }
                if (iref is IIParamBlock2)
                {
                    foreach (var v in EnumerateParamBlock((IIParamBlock2)iref))
                    {
                        yield return v;
                    }
                    continue;
                }
                if (recursive)
                {
                    if (iref is IReferenceMaker)
                    {
                        foreach (var v in EnumerateReferences(iref))
                        {
                            yield return v;
                        }
                        continue;
                    }
                }
            }

        }

        public IEnumerable<Parameter> EnumerateProperties(IReferenceMaker obj)
        {
            //some materials, like standard material, whose paramters change depending on settings, will have paramblocks within containers.
            //we do not want to recurse the entire graph though, or else we will start to enumerate things like texmap references. So the following
            //gets the first level of reference targets, then enumerates all the parameters availble from them and stops there

            foreach (var r in obj.GetReferenceTargets())
            {
                if (r == null)
                {
                    continue; //yes these can be null as well
                }

                if (r is IIParamBlock2)
                {
                    foreach (var p in EnumerateParamBlock((IIParamBlock2)r))
                    {
                        yield return p;
                    }
                    continue;
                }

                if (r is IISubMap)
                {
                    continue;
                }
                if (r is IBitmap)
                {
                    continue;
                }

                foreach(var p in EnumerateParamBlocks(r))
                {
                    yield return p;
                }
            }
        }

        #endregion

        #region Parameter Type

        public class Parameter
        {
            public IIParamBlock2 m_containingBlock;

            public string m_parameterName;
            public string m_internalName;

            public short m_Id;
            public int m_TableId;
            public ParamType2 m_Type;

            public Parameter(ParameterReference portableReference)
            {
                IAnimatable anim = Autodesk.Max.GlobalInterface.Instance.Animatable.GetAnimByHandle(new UIntPtr(portableReference.m_ownerAnimHandle));

                m_containingBlock = anim.GetParamBlockByID(portableReference.m_paramBlockId);
                m_Id = portableReference.m_paramId;
                m_TableId = portableReference.m_tableId;

                m_parameterName = m_containingBlock.GetLocalName(m_Id, m_TableId);
                m_internalName = m_containingBlock.GetParamDef(m_Id).IntName;
                m_Type = m_containingBlock.GetParameterType(m_Id);
            }

            public Parameter(IIParamBlock2 blck, short idx, int tabid)
            {
                m_containingBlock = blck;
                m_Id = idx;
                m_TableId = tabid;

                m_parameterName = m_containingBlock.GetLocalName(m_Id, m_TableId);
                m_internalName = m_containingBlock.GetParamDef(m_Id).IntName;
                m_Type = m_containingBlock.GetParameterType(m_Id);
            }

            public Parameter(IIParamBlock2 blck, IParamAlias alias)
            {
                m_containingBlock = blck;
                m_Id = alias.Id;
                m_TableId = alias.TabIndex;

                m_parameterName = alias.Alias;
                //Internal names are not added for aliases
                m_Type = m_containingBlock.GetParameterType(m_Id);
            }

            public bool IsMapType
            {
                get
                {
                    return (
                        IsTexmapType ||
                        IsBitmapType);
                }
            }

            public bool IsTexmapType
            {
                get
                {
                    switch (m_Type)
                    {
                        case ParamType2.Texmap:
                        case ParamType2.TexmapTab:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            public bool IsBitmapType
            {
                get
                {
                    switch (m_Type)
                    {
                        case ParamType2.Bitmap:
                        case ParamType2.BitmapTab:
                            return true;
                        default:
                            return false;
                    }
                }
            }

            public bool IsValueType
            {
                get { return !IsMapType; }
            }

            public bool SetValue(bool value)
            {
                return m_containingBlock.SetValue(m_Id, 0, value ? 1 : 0, m_TableId);
            }
            public bool SetValue(float value)
            {
                return m_containingBlock.SetValue(m_Id, 0, value, m_TableId);
            }
            public bool SetValue(ITexmap value)
            {
                return m_containingBlock.SetValue(m_Id, 0, value, m_TableId);
            }
            public bool SetValue(IAColor value)
            {
                return m_containingBlock.SetValue(m_Id, 0, value, m_TableId);
            }
            public bool SetValue(IColor value)
            {
                return m_containingBlock.SetValue(m_Id, 0, value, m_TableId);
            }
            public bool SetValue(string value)
            {
                return m_containingBlock.SetValue(m_Id, 0, value, m_TableId);
            }

            public fRGBA GetColour()
            {
                /* If a three component colour is retrieved as a four component colour it will be black. Since the type does not 
                 * indicate which one it is, we get both and check which one should be returned. */

                IAColor ac = m_containingBlock.GetAColor(m_Id, 0, m_TableId);
                IColor c = m_containingBlock.GetColor(m_Id, 0, m_TableId);

                fRGBA colour = c.TofRGBA();

                if (ac.Length() != 0)
                {
                    //at least one component is greater than zero, so this one is valid and we should use this, otherwise even if 
                    //the other one is black, it will turn out the same anyway
                    colour = ac.TofRGBA();
                }

                switch (m_Type)
                {   
                    case ParamType2.Rgba:
                    case ParamType2.Frgba:

                        //should we check if its 0/255 or 0f/1f here?

                        break;
                }

                return colour;
            }

            public string GetString()
            {
                return m_containingBlock.GetStr(m_Id, 0, m_TableId);
            }

            public float GetFloat() //can also return integer types
            {
                switch (m_Type)
                {
                    case ParamType2.Int:
                    case ParamType2.Int64:
                    case ParamType2.Int64Tab:
                        return m_containingBlock.GetInt(m_Id, 0, m_TableId);
                    default:
                        return m_containingBlock.GetFloat(m_Id, 0, m_TableId);
                }
            }

            public bool GetBool()
            {
                return (m_containingBlock.GetInt(m_Id, 0, m_TableId) > 0);
            }

            protected ParameterReference GetPortableReference()
            {
                return new ParameterReference
                {
                    m_ownerAnimHandle = Autodesk.Max.GlobalInterface.Instance.Animatable.GetHandleByAnim(m_containingBlock.Owner).ToUInt64(),
                    m_paramBlockId = m_containingBlock.Id,
                    m_paramId = m_Id,
                    m_tableId = m_TableId
                };
            }

            public MaterialReference GetMaterial()
            {
                return new MaterialReference { m_parameterReference = GetPortableReference() };
            }

            public IPBBitmap GetBitmap()
            {
                return m_containingBlock.GetBitmap(m_Id, 0, m_TableId);
            }

            public ITexmap GetTexmap()
            {
                return m_containingBlock.GetTexmap(m_Id, 0, m_TableId); 
            }

            public MapReference GetMap()
            {
                MapReference map = new MapReference();

                switch (m_Type)
                {
                    case ParamType2.Texmap:
                    case ParamType2.TexmapTab:
                        ITexmap t = m_containingBlock.GetTexmap(m_Id, 0, m_TableId);
                        if (t == null)
                        {
                            return null;
                        }
                        if (t is IBitmapTex)
                        {
                            map.m_mapType = "TexMap"; 
                        }

                        map.m_mapName = t.Name;
                        break;


                    case ParamType2.Bitmap:
                    case ParamType2.BitmapTab:
                        IPBBitmap b = m_containingBlock.GetBitmap(m_Id, 0, m_TableId);
                        if (b == null)
                        {
                            return null;
                        }
                        map.m_mapType = "Bitmap";
                        map.m_mapName = b.Bi.Filename;
                        break;

                    case ParamType2.Filename:
                    case ParamType2.FilenameTab:
                        string fn = m_containingBlock.GetStr(m_Id, 0, m_TableId);

                        if (fn == null)
                        {
                            return null;
                        }

                        map.m_mapType = "Filename";
                        map.m_mapName = fn;
                        break;

                    default:
                        Log.Add("Cannot convert ParamType2: " + m_Type.ToString() + " to Map");
                        return null;
                }

                map.m_parameterReference = GetPortableReference();

                return map;

            }

            public object GetValue()
            {
                switch (m_Type)
                {
                    case ParamType2.Bool:
                    case ParamType2.Bool2:
                    case ParamType2.BoolTab:
                    case ParamType2.BoolTab2:
                        return GetBool();

                    case ParamType2.Frgba:
                    case ParamType2.Rgba:
                        return GetColour();

                    case ParamType2.PcntFrac:
                    case ParamType2.PcntFracTab:
                    case ParamType2.Float:
                    case ParamType2.FloatTab:
                    case ParamType2.Int:
                    case ParamType2.Int64:
                    case ParamType2.IntTab:
                        return GetFloat();

                    case ParamType2.Bitmap:
                    case ParamType2.BitmapTab:
                    case ParamType2.Texmap:
                    case ParamType2.TexmapTab:
                        return GetMap();

                    case ParamType2.Filename:
                    case ParamType2.FilenameTab:
                    case ParamType2.String:
                    case ParamType2.StringTab:
                        return GetString();

                    case ParamType2.Mtl:
                    case ParamType2.MtlTab:
                        return GetMaterial();

                    default:
                        //throw new Exception("Don't know type for ParamType2: " + Type.ToString());
                        Log.Add("Don't know how to get ParamType2: " + m_Type.ToString());
                        return null;
                }
            }

            public string GetValueAsString()
            {
                return GetValue().ToString();
            }           
        }

        #endregion
    }
}
