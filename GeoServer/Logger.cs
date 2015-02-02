using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Web;
using Rtx.Core.Log;
using Rtx.Core.Log.Core;

namespace GeoServer
{
	public static class Logger
	{
		public class LoggingEventData : ILoggingEventData
		{
			public DateTime DateCreated { get; set; }
			public string EventId { get; set; }
			public string Message { get; set; }
			public string Comments { get; set; }

			[SuppressMessage("Microsoft.Usage", "CA2227:CollectionPropertiesShouldBeReadOnly")]
			public IDictionary<string, string> AdditionalInfo { get; set; }

			public Exception Error { get; set; }
		}

		public class ExceptionData : LoggingEventData
		{
			public override string ToString()
			{
				return FormatMessage(this.EventId, this.Comments, this.AdditionalInfo);
			}
			private static string FormatMessage(string errorId, string exceptionComments,
				IDictionary<string, string> additionalErrorInfo)
			{
				StringBuilder result = new StringBuilder();
				result.AppendFormat("Error number: {0}", errorId);
				if (additionalErrorInfo != null)
					AppendDictionary(result, additionalErrorInfo);
				result.AppendLine();
				result.Append(exceptionComments);
				return result.ToString();
			}

			private static void AppendName(StringBuilder sb, string name)
			{
				sb.Append(name);
				sb.Append(": ");
			}
			private static void AppendNull(StringBuilder sb)
			{
				sb.Append("null");
			}
			private static void AppendString(StringBuilder sb, string value)
			{
				sb.Append(value);
			}
			private static void AppendDictionary(StringBuilder sb, IDictionary<string, string> dic)
			{
				foreach (KeyValuePair<string, string> pair in dic)
				{
					sb.AppendLine();
					AppendName(sb, pair.Key);
					if (pair.Value == null)
						AppendNull(sb);
					else
						AppendString(sb, pair.Value);
				}
			}
		}
		private const string ErrorIdStoragePrefix = "errorId:";

		public static string Log(string message, Exception exception)
		{
			return Log("Emergency", exception, message);
		}

		public static void LogCall(string message)
		{
			LogManager.GetLogger("Call").Info(message);
		}

		private static string Log(string loggerName, Exception exception, string message)
		{
			try
			{
				string errorId = ProvideErrorId(exception);
				ExceptionData errorMessage = new ExceptionData
				{
					DateCreated = DateTime.Now,
					EventId = errorId,
					Error = exception,
					Message = exception == null ? message : exception.Message,
					Comments = FormatExceptionComments(exception, message),
					AdditionalInfo = GetLogDetails(),
				};
				LogManager.GetLogger(loggerName).Error(errorMessage);
				return errorId;
			}
			catch (Exception ex)
			{
				LogManager.GetLogger("Emergency").Error(
					"Logger.Log error:", ex);
			}
			return "invalid_error_id";
		}

		private static IDictionary<string, string> GetLogDetails()
		{
			if (null == HttpContext.Current || null == HttpContext.Current.Request)
				return null;

			HttpRequest request = HttpContext.Current.Request;
			Dictionary<string, string> result = new Dictionary<string, string>();
			result.Add("Url", request.Url.AbsoluteUri);
			result.Add("UrlReferrer", (null == request.UrlReferrer) ? String.Empty : request.UrlReferrer.AbsoluteUri);
			result.Add("RequestMethod", request.HttpMethod);
			result.Add("UserAgent", request.UserAgent);
			result.Add("IP", GetUserHostAddress(request));
			return result;
		}

		public static string ProvideErrorId(Exception exception)
		{
			if (exception == null)
				return String.Empty;

			string errorId = null;
			if (!String.IsNullOrEmpty(exception.HelpLink)
				&& exception.HelpLink.StartsWith(ErrorIdStoragePrefix, StringComparison.Ordinal))
			{
				errorId = exception.HelpLink.Remove(0, ErrorIdStoragePrefix.Length);
			}
			else
			{
				errorId = Guid.NewGuid().ToString();
				exception.HelpLink = ErrorIdStoragePrefix + errorId;
			}
			return errorId;
		}

		private static string GetUserHostAddress(HttpRequest request)
		{
			try
			{
				return request.UserHostAddress;
			}
			catch (Exception)
			{
				return "unknown";
			}
		}
		//
		public static string FormatExceptionComments(Exception error, string exceptionComments)
		{
			StringBuilder result = new StringBuilder();
			FormatException(result, error, "===", exceptionComments);
			return result.ToString();
		}

		private static void FormatException(StringBuilder result, Exception ex, string exceptionGroupPrefix,
			string exceptionComments)
		{
			if (!string.IsNullOrEmpty(exceptionComments))
				result.Append(exceptionGroupPrefix).Append(' ').Append(exceptionComments).Append(' ');
			result.Append(exceptionGroupPrefix);

			if (ex == null) return;

			result.AppendLine(">");

			result.AppendLine("ExceptionType: " + ex.GetType().FullName);
			result.AppendLine("Message: " + ex.Message);
			if (ex.InnerException == null)
				result.AppendLine("StackTrace:");
			else
			{
				FormatException(result, ex.InnerException, "---", "Inner exception");
				result.Append("   ").AppendLine("--- End of inner exception stack trace ---");
			}
			result.AppendLine(ex.StackTrace);
		}
	}
}