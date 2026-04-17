// using System.Text;
// using Application.Extensions;
// using Microsoft.AspNetCore.Http;
// using Microsoft.EntityFrameworkCore;
// using MultipleBotFramework;
// using MultipleBotFramework.Constants;
// using MultipleBotFramework.Db;
// using MultipleBotFramework.Db.Entity;
// using MultipleBotFramework.Extensions;
// using MultipleBotFramework.Services.Interfaces;
// using Shared.Const;
// using Telegram.BotAPI.AvailableMethods;
// using InputFile = Telegram.BotAPI.AvailableTypes.InputFile;
// using Message = Telegram.BotAPI.AvailableTypes.Message;
//
// namespace Application.Utils;
//
// public class ExceptionReporter
// {
//     public static async Task Report(BotDbContext botDb, IBotsManagerService botManager, Exception? e = null, string? log = null, string title = "Exception", string? additionalData = null, HttpContext? context = null)
//     {
//         var botClient = await botManager.GetBotClientById(AppConstants.BotId);
//         IEnumerable<BotChatEntity> exceptionChats =
//             await botDb.Chats.Where(c => c.Tags != null && c.Tags.Contains(BotConstants.ModerationChatTags.Exception)).ToListAsync();
//
//         if (exceptionChats.Any() == false) return;
//
//         StringBuilder sb = new StringBuilder();
//         if (e != null)
//         {
//             sb.AppendLine($"Exception: {e.Message}\n");
//             sb.AppendLine($"StackTrace: {e.StackTrace}\n");
//         }
//
//         if (log != null)
//         {
//             sb.AppendLine($"Log: {log}\n");
//         }
//
//         if (context != null)
//         {
//             sb.AppendLine($"Request.Method: {context.Request.Method}\n");
//             sb.AppendLine($"Request.Path: {context.Request.Path.Value}\n");
//             sb.AppendLine($"Request.Query: {context.Request.Query.ToString()}\n");
//
//             if (context.Request.Headers.ContainsKey("Authorization"))
//             {
//                 sb.AppendLine($"Request.TokenData: \n{context.TokenData().ToJson()}\n");
//             }
//
//             // string bodyStr = await ReadRequestBodyAsync(context);
//             // sb.AppendLine($"Request.Body: {bodyStr}\n");
//         }
//
//         if (string.IsNullOrEmpty(additionalData) == false)
//         {
//             sb.AppendLine($"Additional data:\n{additionalData}\n");
//         }
//
//         StringBuilder caption = new();
//         caption.AppendLine($"<b>{title}</b>");
//
//         if(context != null)
//             caption.AppendLine($"<b>Path: </b>" + context.Request.Path.Value);
//
//         if(e is not null)
//             caption.AppendLine($"<b>Exception: </b>" + e?.Message);
//
//         string captionStr = caption.ToString();
//         if (captionStr.Length > 800) captionStr = captionStr.Substring(0, 800);
//         string fileFromTelegram = null;
//
//         using (Stream stream = GenerateStreamFromString(sb.ToString()))
//         {
//             string errorFileName = $"{DateTime.Now.Ticks.ToString()}.txt";
//             InputFile fileException = new InputFile(stream, errorFileName);
//
//             foreach (BotChatEntity ch in exceptionChats)
//             {
//                 try
//                 {
//                     Message message = null;
//                     if (string.IsNullOrEmpty(fileFromTelegram) == false)
//                     {
//                         message = await botClient.SendDocumentAsync(ch.ChatId, fileFromTelegram, caption: captionStr,
//                             parseMode: ParseMode.Html);
//                     }
//                     else
//                     {
//                         message = await botClient.SendDocumentAsync(ch.ChatId, fileException, caption: captionStr,
//                             parseMode: ParseMode.Html);
//                     }
//
//                     // Другим отрпавляем тот же документ, только по ИД.
//                     fileFromTelegram = message.Document!.FileId;
//                 }
//                 catch (Exception exception)
//                 {
//                     // Не смогли отправить сообщение в Telegram.
//                 }
//
//             }
//         }
//     }
//     private static Stream GenerateStreamFromString(string s)
//     {
//         var stream = new MemoryStream();
//         var writer = new StreamWriter(stream);
//         writer.Write(s);
//         writer.Flush();
//         stream.Position = 0;
//         return stream;
//     }
//
//     public static async Task<string> ReadRequestBodyAsync(HttpContext context)
//     {
//         context.Request.EnableBuffering(); // позволяет повторно читать Body
//
//         context.Request.Body.Position = 0;
//
//         string jsonContent;
//         using (var reader = new StreamReader(context.Request.Body, System.Text.Encoding.UTF8))
//         {
//             jsonContent = await reader.ReadToEndAsync();
//         }
//
//         context.Request.Body.Position = 0; // сбросить позицию, чтобы Body можно было прочитать ещё раз
//         return jsonContent;
//     }
// }
