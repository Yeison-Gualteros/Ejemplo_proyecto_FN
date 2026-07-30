using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Net.Http.Headers;
using Shared.DataTransferObjects;
using System.Text;

namespace FileNova
{
    public class CsvOutputFormatter : TextOutputFormatter
    {
        public CsvOutputFormatter()
        {
            SupportedMediaTypes.Add(MediaTypeHeaderValue.Parse("text/csv"));
            SupportedEncodings.Add(Encoding.UTF8);
            SupportedEncodings.Add(Encoding.Unicode);
        }

        public override async Task WriteResponseBodyAsync(OutputFormatterWriteContext context, Encoding selectedEncoding)
        {
            var response = context.HttpContext.Response;
            var buffer = new StringBuilder();

            if (context.Object is IEnumerable<object> list)
            {
                foreach (var item in list)
                {
                    var values = item.GetType()
                        .GetProperties()
                        .Select(p => p.GetValue(item)?.ToString()?.Replace(",", " "));

                    buffer.AppendLine(string.Join(",", values));
                }
            }
            else
            {
                var values = context.Object.GetType()
                    .GetProperties()
                    .Select(p => p.GetValue(context.Object)?.ToString()?.Replace(",", " "));

                buffer.AppendLine(string.Join(",", values));
            }

            await response.WriteAsync(buffer.ToString());
        }
    }
}
