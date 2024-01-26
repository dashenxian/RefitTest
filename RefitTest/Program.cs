using System.ComponentModel.DataAnnotations;
using Refit;

namespace RefitTest
{
    internal class Program
    {
        static async Task<int> Main(string[] args)
        {
            var tasks = new List<Task>();
            for (int i = 0; i < 1; i++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    await Upload();
                }));
            }

            await Task.WhenAll(tasks);

            return 0;
        }

        private static async Task Upload()
        {
            var url = "http://localhost:5163/";
            //var gitApi = RestService.For<IGitHubApi>(url);
            var multiApi = RestService.For<IMultiPartUploadFileApi>(url);
            var file = new System.IO.FileStream(@"C:\Users\Administrator\Desktop\3dloupan\510521999999GB00122 (0)\510521999999GB00122 (1).zip", FileMode.Open, FileAccess.Read);
            var totalLength = file.Length;
            var partCount = 10;
            var partLength = (int)(totalLength / partCount);
            using var httpclient = new HttpClient();
            var id = Guid.NewGuid().ToString();
            for (int i = 0; i < partCount; i++)
            {
                var currentPartLength = partLength;
                if (i == partCount - 1)
                {
                    currentPartLength = (int)(totalLength - partLength * (partCount - 1));
                }
                var content = new byte[currentPartLength];
                file.Read(content, 0, currentPartLength);
                //using var ms = new MemoryStream(content);
                var input = new MultiPartUploadFileDefaultInput()
                {
                    FileName = Path.GetFileName(file.Name),
                    StartByteIndex = i * partLength,
                    File = new ByteArrayPart(content, fileName: Path.GetFileName(file.Name)),
                    TotalLength = totalLength,
                    Id = id,
                };
                var result = await multiApi.MultiPartUploadFile(input.File, input.StartByteIndex, input.TotalLength, input.FileName, input.Id);
                //var result = await multiApi.MultiPartUploadFile(input);
                Console.WriteLine(result);
                //await Send(httpclient,input);
            }
        }

        public static async Task Send(HttpClient httpClient, MultiPartUploadFileDefaultInput input)
        {
            // 发起请求
            var apiUrl = "http://localhost:5163/MultiPartUploadFile";
            using (var content = new MultipartFormDataContent())
            {
                // 添加表单字段
                //content.Add(new ByteArrayContent(input.File), "File",input.FileName);
                content.Add(new StringContent(input.StartByteIndex.ToString()), "StartByteIndex");
                content.Add(new StringContent(input.TotalLength.ToString()), "TotalLength");
                //content.Add(new StringContent(input.FileName), "FileName");

                // 发起 POST 请求
                var response = await httpClient.PostAsync(apiUrl, content);

                // 处理响应
                if (response.IsSuccessStatusCode)
                {
                    string responseContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine("请求成功：" + responseContent);
                }
                else
                {
                    Console.WriteLine("请求失败：" + response.StatusCode);
                }
            }

        }
    }
    public interface IMultiPartUploadFileApi
    {
        [Multipart]
        [Post("/MultiPartUploadFile")]
        //Task<string> MultiPartUploadFile(MultiPartUploadFileDefaultInput input);
        Task<string> MultiPartUploadFile([AliasAs(nameof(MultiPartUploadFileDefaultInput.File))] MultipartItem File, long StartByteIndex, long TotalLength, string FileName, string id);
    }

    public class MultiPartUploadFileDefaultInput
    {
        /// <summary>
        /// 文件内容
        /// </summary>
        [Required]
        public required MultipartItem File { get; set; }
        /// <summary>
        /// 当前分段在全文件的开始字节位置
        /// </summary>
        [Required]
        public long StartByteIndex { get; set; }
        /// <summary>
        /// 文件总长度
        /// </summary>
        [Required]
        public long TotalLength { get; set; }
        /// <summary>
        /// 文件名称
        /// </summary>
        [Required]
        public required string FileName { get; set; }
        /// <summary>
        /// 标识码，同一个文件多个分段应该一样，请使用随机值，避免与其他文件相同造成错误合并到其他文件的分段
        /// </summary>
        [Required]
        public required string Id { get; set; }
    }
}
