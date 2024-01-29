using Microsoft.AspNetCore.Http;
using midelware.Singleton.Logger;
using System.Buffers;
using System.IdentityModel.Tokens.Jwt;
using System.IO.Pipelines;
using System.Net.Http;
using System.Reflection.PortableExecutable;
using System.Text;

namespace midelware.Middlewares
{
    public class MiMiddleware
    {

        private readonly RequestDelegate _next;

        public MiMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Lógica del middleware antes de pasar al siguiente en la cadena

            StringBuilder stringBuilder = new StringBuilder();
            StringBuilder headerBuilder = new StringBuilder();

            try
            {

                // Interceptando y leyendo la solicitud
                //------------------------------------------------------------------
                context.Request.EnableBuffering();
                var requestStream = new StreamReader(context.Request.Body);
                string requestBody = await requestStream.ReadToEndAsync();
                context.Request.Body.Position = 0;

                foreach (var header in context.Request.Headers)
                    headerBuilder.AppendLine($"  {header.Key}: {header.Value}");
                //------------------------------------------------------------------

                // Se alamacena informacion de la transaccion en trada
                //------------------------------------------------------------------
                stringBuilder.AppendLine("");
                stringBuilder.AppendLine("------------------------------------------------------------");
                stringBuilder.AppendLine("Request");
                stringBuilder.AppendLine("_______________________________________");
                stringBuilder.AppendLine($"Method: {context.Request.Method}");
                stringBuilder.AppendLine($"Headers: {headerBuilder.ToString()}");
                stringBuilder.AppendLine($"HttpContext: {context.Request.Path}");
                stringBuilder.AppendLine($"body: {requestBody}");
                stringBuilder.AppendLine($"");
                //------------------------------------------------------------------


                // Si llega un token, valida que no este vencido
                //------------------------------------------------------------------
                var token = context.Request.Headers["Authorization"].ToString().Replace("Bearer ", "");

                if (!string.IsNullOrEmpty(token))
                {
                    var jwtToken = new JwtSecurityToken(token);
                    var expira = jwtToken.ValidTo;

                    if (expira < DateTime.UtcNow)
                    {
                        context.Response.StatusCode = 401; // Unauthorized

                        stringBuilder.AppendLine($"Response");
                        stringBuilder.AppendLine($"_______________________________________");
                        stringBuilder.AppendLine($"status code: {context.Response.StatusCode}");                     
                        stringBuilder.AppendLine($"------------------------------------------------------------");
                        AppLogger.GetInstance().Info(stringBuilder.ToString());

                        return; // Detiene la ejecución del middleware
                    }
                }
                //------------------------------------------------------------------

                // Interceptando la respuesta
                //------------------------------------------------------------------
                var originalBodyStream = context.Response.Body;
                using var responseBodyStream = new MemoryStream();
                context.Response.Body = responseBodyStream;
                //------------------------------------------------------------------

                // Llamada al siguiente middleware en la cadena
                await _next(context);               

                //se Obtiene el body de respuesta
                responseBodyStream.Seek(0, SeekOrigin.Begin);
                string responseBody = await new StreamReader(responseBodyStream).ReadToEndAsync();
                responseBodyStream.Seek(0, SeekOrigin.Begin);

                // Se alamacena informacion de la transaccion de salida
                //------------------------------------------------------------------
                stringBuilder.AppendLine($"Response");
                stringBuilder.AppendLine($"_______________________________________");
                stringBuilder.AppendLine($"status code: {context.Response.StatusCode}");
                stringBuilder.AppendLine($"body: {responseBody}");
                stringBuilder.AppendLine($"------------------------------------------------------------");
                //------------------------------------------------------------------


                // Registra la transaccion en el log
                AppLogger.GetInstance().Info(stringBuilder.ToString());

                // Copiando de nuevo el stream de respuesta al original
                await responseBodyStream.CopyToAsync(originalBodyStream);

            }
            catch (Exception e)
            {
                context.Response.StatusCode = 500; // Error server
                stringBuilder.AppendLine($"Error");
                stringBuilder.AppendLine($"_______________________________________");
                stringBuilder.AppendLine($"{e}");
                stringBuilder.AppendLine($"");
                stringBuilder.AppendLine($"Response");
                stringBuilder.AppendLine($"_______________________________________");
                stringBuilder.AppendLine($"status code: {context.Response.StatusCode}");
                stringBuilder.AppendLine($"------------------------------------------------------------");

                AppLogger.GetInstance().Info(stringBuilder.ToString());                
                return;
            }


        }

    }
}
