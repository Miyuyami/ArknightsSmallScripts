using System;
using System.IO;

namespace ExportFurnitureToCsv
{
    public static class Extensions
    {
        public static void WriteCsvRow(this TextWriter textWriter, params object[] data)
        {
            textWriter.WriteLine(String.Join(',', data));
        }
    }
}
