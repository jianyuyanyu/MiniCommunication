using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace MiniSocket.Transmitting
{
    public class BytesConvert
    {
        public static byte[] ObjectToBytes(object obj)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                try
                {
                    binaryFormatter.Serialize(memoryStream, obj);
                }
                catch
                {
                    return null;
                }
                return memoryStream.ToArray();
            }
        }

        public static object BytesToObject(byte[] bytes, int effectiveByte, string namespaceName)
        {
            using (MemoryStream memoryStream = new MemoryStream(bytes, 0, effectiveByte))
            {
                BinaryFormatter binaryFormatter = new BinaryFormatter();
                binaryFormatter.Binder = new Binder(namespaceName);
                try
                {
                    return binaryFormatter.Deserialize(memoryStream);
                }
                catch
                {
                    return null;
                }
            }
        }

        class Binder : SerializationBinder
        {
            private string _namespaceName;

            public Binder(string namespaceName)
            {
                _namespaceName = namespaceName;
            }

            public override Type BindToType(string assemblyName, string typeName)
            {
                string[] stringArray = typeName.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
                typeName = stringArray[stringArray.Length - 1];
                return Type.GetType(_namespaceName + "." + typeName, true);
            }
        }
    }
}