using System;

namespace Voyager.Configuration.MountPath
{
	/// <summary>
	/// Exception thrown when configuration loading fails.
	/// </summary>
	public class ConfigurationException : Exception
	{
		/// <summary>
		/// Gets the mount path where configuration was being loaded from.
		/// </summary>
		public string? MountPath { get; }

		/// <summary>
		/// Gets the filename that was being loaded.
		/// </summary>
		public string? FileName { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		public ConfigurationException(string message)
			: base(message)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="innerException">The inner exception.</param>
		public ConfigurationException(string message, Exception innerException)
			: base(message, innerException)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConfigurationException"/> class.
		/// </summary>
		/// <param name="message">The error message.</param>
		/// <param name="mountPath">The mount path where configuration was being loaded from.</param>
		/// <param name="fileName">The filename that was being loaded.</param>
		/// <param name="innerException">The inner exception.</param>
		public ConfigurationException(
			string message,
			string? mountPath,
			string? fileName,
			Exception? innerException = null)
			: base(message, innerException)
		{
			MountPath = mountPath;
			FileName = fileName;
		}
	}
}
