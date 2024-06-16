using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.TypeConversion;
using CTO.Price.Shared.Domain;
using CTO.Price.Shared.Extensions;
using Grpc.Core;

namespace CTO.Price.Shared.Services
{
    public static class CsvProvider
    {
        /// <summary>
        /// Get list of UploadedPrice from byte[]
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static IEnumerable<UploadedPrice> GetPrices(byte[] bytes)
        {
            var endLine = new[] {(byte) 13, (byte) 10};
            if (bytes.CountMatchData(endLine) > 20000)
                throw new RpcException(new Status(StatusCode.InvalidArgument, "records are over limitation"));
            
            if (IsContainEmptyRow(bytes, 9))
                throw new RpcException(new Status(StatusCode.InvalidArgument, "upload file contain empty record"));

            using var memoryStream = new MemoryStream(bytes);
            using var reader = new StreamReader(memoryStream);
            using var csvReader = new CsvReader(reader, CultureInfo.CurrentCulture);
            csvReader.Read();
            csvReader.ReadHeader();
            csvReader.ValidateHeader(typeof(UploadedPrice));
            var records = new List<UploadedPrice>();
            var invalids = new List<string>();
            var row = 1;

            while (csvReader.Read() && invalids.Count < 10) {
                try {
                    var record = csvReader.GetRecord<UploadedPrice>();
                    records.Add(record);
                }
                catch (TypeConverterException ex) {
                    invalids.Add($"row {row} : The conversion cannot be performed for data '{ex.Text}'");
                }
                catch (Exception ex) {
                    invalids.Add($"row {row} : {ex.Message}");
                }

                row++;
            }
            
            if (invalids.Any())
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument,
                    $"Invalid payload detected at row(s)|{string.Join("|", invalids)}" +
                    (invalids.Count == 10 ? "|and may have more..." : string.Empty)));
            }

            return records;
        }
        
        static bool IsContainEmptyRow(byte[] bytes, int numberOfColumn)
        {
            var emptyRowByte = Enumerable.Repeat((byte) 44, numberOfColumn - 1).ToArray();
            return bytes.CountMatchData(emptyRowByte) > 0;
        }
    }
}