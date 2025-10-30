using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SilkBound.Types.Data {
    public struct SerializerContext {
        public BinaryWriter? Writer;
        public BinaryReader? Reader;

        // just gunna make these singletons since it cant be parallel anyways. less reallocations for the already overcrowded heap lol
        private static SerializerContext serializeContext = new SerializerContext();
        private static SerializerContext deserializeContext = new SerializerContext();

        public static SerializerContext GetSerializer(BinaryWriter writer)
        {
            serializeContext.Writer = writer;
            serializeContext.Reader = null;

            return serializeContext;
        }
        public static SerializerContext GetDeserializer(BinaryReader reader)
        {
            deserializeContext.Writer = null;
            deserializeContext.Reader = reader;

            return deserializeContext;
        }
    }
}
