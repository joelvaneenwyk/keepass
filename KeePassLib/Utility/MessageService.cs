/*
  KeePass Password Safe - The Open-Source Password Manager
  Copyright (C) 2003-2024 Dominik Reichl <dominik.reichl@t-online.de>

  This program is free software; you can redistribute it and/or modify
  it under the terms of the GNU General Public License as published by
  the Free Software Foundation; either version 2 of the License, or
  (at your option) any later version.

  This program is distributed in the hope that it will be useful,
  but WITHOUT ANY WARRANTY; without even the implied warranty of
  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
  GNU General Public License for more details.

  You should have received a copy of the GNU General Public License
  along with this program; if not, write to the Free Software
  Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using KeePassLib.Resources;
using KeePassLib.Serialization;
using System;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Text;
using System.Threading;

#if !KeePassUAP
using System.Windows.Forms;
#endif

namespace KeePassLib.Utility
{
#if KeePassUAP
	/// <summary>Specifies constants defining which buttons to display on a <see cref="T:System.Windows.Forms.MessageBox" />.</summary>
	public enum MessageBoxButtons
	{
		/// <summary>The message box contains an OK button.</summary>
		OK,
		/// <summary>The message box contains OK and Cancel buttons.</summary>
		OKCancel,
		/// <summary>The message box contains Abort, Retry, and Ignore buttons.</summary>
		AbortRetryIgnore,
		/// <summary>The message box contains Yes, No, and Cancel buttons.</summary>
		YesNoCancel,
		/// <summary>The message box contains Yes and No buttons.</summary>
		YesNo,
		/// <summary>The message box contains Retry and Cancel buttons.</summary>
		RetryCancel,
		/// <summary>Specifies that the message box contains Cancel, Try Again, and Continue buttons.</summary>
		CancelTryContinue,
	}

	/// <summary>Specifies constants defining which information to display.</summary>
	public enum MessageBoxIcon
	{
		/// <summary>The message box contains no symbols.</summary>
		None = 0,
		/// <summary>The message box contains a symbol consisting of white X in a circle with a red background.</summary>
		Error = 16, // 0x00000010
		/// <summary>The message box contains a symbol consisting of a white X in a circle with a red background.</summary>
		Hand = 16, // 0x00000010
		/// <summary>The message box contains a symbol consisting of white X in a circle with a red background.</summary>
		Stop = 16, // 0x00000010
		/// <summary>The message box contains a symbol consisting of a question mark in a circle. The question mark message icon is no longer recommended because it does not clearly represent a specific type of message and because the phrasing of a message as a question could apply to any message type. In addition, users can confuse the question mark symbol with a help information symbol. Therefore, do not use this question mark symbol in your message boxes. The system continues to support its inclusion only for backward compatibility.</summary>
		Question = 32, // 0x00000020
		/// <summary>The message box contains a symbol consisting of an exclamation point in a triangle with a yellow background.</summary>
		Exclamation = 48, // 0x00000030
		/// <summary>The message box contains a symbol consisting of an exclamation point in a triangle with a yellow background.</summary>
		Warning = 48, // 0x00000030
		/// <summary>The message box contains a symbol consisting of a lowercase letter i in a circle.</summary>
		Asterisk = 64, // 0x00000040
		/// <summary>The message box contains a symbol consisting of a lowercase letter i in a circle.</summary>
		Information = 64, // 0x00000040
	}

	/// <summary>Specifies options on a <see cref="T:System.Windows.Forms.MessageBox" />.</summary>
	[Flags]
	public enum MessageBoxOptions
	{
		/// <summary>The message box is displayed on the active desktop. The caller is a service notifying the user of an event.</summary>
		ServiceNotification = 2097152, // 0x00200000
		/// <summary>The message box is displayed on the active desktop. This constant is similar to ServiceNotification, except that the system displays the message box only on the default desktop of the interactive window station. The application that displayed the message box loses focus, and the message box is displayed without using visual styles. For more information, see Rendering Controls with Visual Styles.</summary>
		DefaultDesktopOnly = 131072, // 0x00020000
		/// <summary>The message box text is right-aligned.</summary>
		RightAlign = 524288, // 0x00080000
		/// <summary>Specifies that the message box text is displayed with right to left reading order.</summary>
		RtlReading = 1048576, // 0x00100000
	}
#endif

	public sealed class MessageServiceEventArgs : EventArgs
	{
		private readonly string m_strTitle = string.Empty;
		public string Title { get { return m_strTitle; } }

		private readonly string m_strText = string.Empty;
		public string Text { get { return m_strText; } }

		private readonly MessageBoxButtons m_mbb = MessageBoxButtons.OK;
		public MessageBoxButtons Buttons { get { return m_mbb; } }

		private readonly MessageBoxIcon m_mbi = MessageBoxIcon.None;
		public MessageBoxIcon Icon { get { return m_mbi; } }

		public MessageServiceEventArgs() { }

		public MessageServiceEventArgs(string strTitle, string strText,
			MessageBoxButtons mbb, MessageBoxIcon mbi)
		{
			m_strTitle = (strTitle ?? string.Empty);
			m_strText = (strText ?? string.Empty);
			m_mbb = mbb;
			m_mbi = mbi;
		}
	}

	public static class MessageService
	{
		private static int g_nCurrentMessageCount = 0;

#if !KeePassLibSD
		private const MessageBoxIcon g_mbiInfo = MessageBoxIcon.Information;
		private const MessageBoxIcon g_mbiWarning = MessageBoxIcon.Warning;
		private const MessageBoxIcon g_mbiFatal = MessageBoxIcon.Error;

		private const MessageBoxOptions g_mboRtl = (MessageBoxOptions.RtlReading |
			MessageBoxOptions.RightAlign);
#else
		private const MessageBoxIcon g_mbiInfo = MessageBoxIcon.Asterisk;
		private const MessageBoxIcon g_mbiWarning = MessageBoxIcon.Exclamation;
		private const MessageBoxIcon g_mbiFatal = MessageBoxIcon.Hand;
#endif
		private const MessageBoxIcon g_mbiQuestion = MessageBoxIcon.Question;

		public static string NewLine
		{
#if !KeePassLibSD
			get { return Environment.NewLine; }
#else
			get { return "\r\n"; }
#endif
		}

		public static string NewParagraph
		{
#if !KeePassLibSD
			get { return (Environment.NewLine + Environment.NewLine); }
#else
			get { return "\r\n\r\n"; }
#endif
		}

		public static uint CurrentMessageCount
		{
			get { return (uint)g_nCurrentMessageCount; }
		}

#if !KeePassUAP
		public static event EventHandler<MessageServiceEventArgs> MessageShowing;
#endif

		private static string ObjectsToMessage(object[] vLines)
		{
			return ObjectsToMessage(vLines, PwDefs.DebugMode);
		}

		private static string ObjectsToMessage(object[] vLines, bool bFullExceptions)
		{
			if (vLines == null) return string.Empty;

			StringBuilder sb = new StringBuilder();
			string strNP = MessageService.NewParagraph;

			foreach (object o in vLines)
			{
				if (o == null) continue;

				string str = (o as string);
				if (str != null) { StrUtil.AppendTrim(sb, strNP, str); continue; }

				Exception ex = (o as Exception);
				if (ex != null)
				{
					StrUtil.AppendTrim(sb, strNP, StrUtil.FormatException(ex,
						bFullExceptions));
					continue;
				}

#if !KeePassLibSD
				StringCollection sc = (o as StringCollection);
				if (sc != null)
				{
					int cchPreSC = sb.Length;
					foreach (string strItem in sc)
						StrUtil.AppendTrim(sb, ((sb.Length == cchPreSC) ?
							strNP : MessageService.NewLine), strItem);
					continue;
				}
#endif

				StrUtil.AppendTrim(sb, strNP, o.ToString());
			}

			return sb.ToString();
		}

#if (!KeePassLibSD && !KeePassUAP)
		internal static Form GetTopForm()
		{
			FormCollection fc = Application.OpenForms;
			if ((fc == null) || (fc.Count == 0)) return null;

			return fc[fc.Count - 1];
		}
#endif

#if !KeePassUAP
		internal static DialogResult SafeShowMessageBox(string strText, string strTitle,
			MessageBoxButtons mbb, MessageBoxIcon mbi, MessageBoxDefaultButton mbdb)
		{
			// strText += MessageService.NewParagraph + (new StackTrace(true)).ToString();

#if KeePassLibSD
			return MessageBox.Show(strText, strTitle, mbb, mbi, mbdb);
#else
			IWin32Window wnd = null;
			try
			{
				Form f = GetTopForm();
				if ((f != null) && f.InvokeRequired)
					return (DialogResult)f.Invoke(new SafeShowMessageBoxInternalDelegate(
						SafeShowMessageBoxInternal), f, strText, strTitle, mbb, mbi, mbdb);
				else wnd = f;
			}
			catch (Exception) { Debug.Assert(false); }

			if (wnd == null)
			{
				if (StrUtil.RightToLeft)
					return MessageBox.Show(strText, strTitle, mbb, mbi, mbdb, g_mboRtl);
				return MessageBox.Show(strText, strTitle, mbb, mbi, mbdb);
			}

			try
			{
				if (StrUtil.RightToLeft)
					return MessageBox.Show(wnd, strText, strTitle, mbb, mbi, mbdb, g_mboRtl);
				return MessageBox.Show(wnd, strText, strTitle, mbb, mbi, mbdb);
			}
			catch (Exception) { Debug.Assert(false); }

			if (StrUtil.RightToLeft)
				return MessageBox.Show(strText, strTitle, mbb, mbi, mbdb, g_mboRtl);
			return MessageBox.Show(strText, strTitle, mbb, mbi, mbdb);
#endif
		}

#if !KeePassLibSD
		internal delegate DialogResult SafeShowMessageBoxInternalDelegate(IWin32Window iParent,
			string strText, string strTitle, MessageBoxButtons mbb, MessageBoxIcon mbi,
			MessageBoxDefaultButton mbdb);

		internal static DialogResult SafeShowMessageBoxInternal(IWin32Window iParent,
			string strText, string strTitle, MessageBoxButtons mbb, MessageBoxIcon mbi,
			MessageBoxDefaultButton mbdb)
		{
			if (StrUtil.RightToLeft)
				return MessageBox.Show(iParent, strText, strTitle, mbb, mbi, mbdb, g_mboRtl);
			return MessageBox.Show(iParent, strText, strTitle, mbb, mbi, mbdb);
		}
#endif

		public static void ShowInfo(params object[] vLines)
		{
			ShowInfoEx(null, vLines);
		}

		public static void ShowInfoEx(string strTitle, params object[] vLines)
		{
			Interlocked.Increment(ref g_nCurrentMessageCount);

			strTitle = (strTitle ?? PwDefs.ShortProductName);
			string strText = ObjectsToMessage(vLines);

			if (MessageService.MessageShowing != null)
				MessageService.MessageShowing(null, new MessageServiceEventArgs(
					strTitle, strText, MessageBoxButtons.OK, g_mbiInfo));

			SafeShowMessageBox(strText, strTitle, MessageBoxButtons.OK, g_mbiInfo,
				MessageBoxDefaultButton.Button1);

			Interlocked.Decrement(ref g_nCurrentMessageCount);
		}

		public static void ShowWarning(params object[] vLines)
		{
			ShowWarningPriv(vLines, PwDefs.DebugMode);
		}

		internal static void ShowWarningExcp(params object[] vLines)
		{
			ShowWarningPriv(vLines, true);
		}

		private static void ShowWarningPriv(object[] vLines, bool bFullExceptions)
		{
			Interlocked.Increment(ref g_nCurrentMessageCount);

			string strTitle = PwDefs.ShortProductName;
			string strText = ObjectsToMessage(vLines, bFullExceptions);

			if (MessageService.MessageShowing != null)
				MessageService.MessageShowing(null, new MessageServiceEventArgs(
					strTitle, strText, MessageBoxButtons.OK, g_mbiWarning));

			SafeShowMessageBox(strText, strTitle, MessageBoxButtons.OK, g_mbiWarning,
				MessageBoxDefaultButton.Button1);

			Interlocked.Decrement(ref g_nCurrentMessageCount);
		}

		public static void ShowFatal(params object[] vLines)
		{
			Interlocked.Increment(ref g_nCurrentMessageCount);

			string strTitle = PwDefs.ShortProductName + " - " + KLRes.FatalError;
			string strText = KLRes.FatalErrorText + MessageService.NewParagraph +
				KLRes.ErrorInClipboard + MessageService.NewParagraph +
				// Please send it to the KeePass developers.
				// KLRes.ErrorFeedbackRequest + MessageService.NewParagraph +
				ObjectsToMessage(vLines, false);

			try
			{
				string strDetails = ObjectsToMessage(vLines, true);

#if KeePassLibSD
				Clipboard.SetDataObject(strDetails);
#else
				Clipboard.Clear();
				Clipboard.SetText(strDetails);
#endif
			}
			catch (Exception) { Debug.Assert(false); }

			if (MessageService.MessageShowing != null)
				MessageService.MessageShowing(null, new MessageServiceEventArgs(
					strTitle, strText, MessageBoxButtons.OK, g_mbiFatal));

			SafeShowMessageBox(strText, strTitle, MessageBoxButtons.OK, g_mbiFatal,
				MessageBoxDefaultButton.Button1);

			Interlocked.Decrement(ref g_nCurrentMessageCount);
		}

		public static DialogResult Ask(string strText, string strTitle,
			MessageBoxButtons mbb)
		{
			Interlocked.Increment(ref g_nCurrentMessageCount);

			string strTextEx = (strText ?? string.Empty);
			string strTitleEx = (strTitle ?? PwDefs.ShortProductName);

			if (MessageService.MessageShowing != null)
				MessageService.MessageShowing(null, new MessageServiceEventArgs(
					strTitleEx, strTextEx, mbb, g_mbiQuestion));

			DialogResult dr = SafeShowMessageBox(strTextEx, strTitleEx, mbb,
				g_mbiQuestion, MessageBoxDefaultButton.Button1);

			Interlocked.Decrement(ref g_nCurrentMessageCount);
			return dr;
		}

		public static bool AskYesNo(string strText, string strTitle, bool bDefaultToYes,
			MessageBoxIcon mbi)
		{
			Interlocked.Increment(ref g_nCurrentMessageCount);

			string strTextEx = (strText ?? string.Empty);
			string strTitleEx = (strTitle ?? PwDefs.ShortProductName);

			if (MessageService.MessageShowing != null)
				MessageService.MessageShowing(null, new MessageServiceEventArgs(
					strTitleEx, strTextEx, MessageBoxButtons.YesNo, mbi));

			DialogResult dr = SafeShowMessageBox(strTextEx, strTitleEx,
				MessageBoxButtons.YesNo, mbi, bDefaultToYes ?
				MessageBoxDefaultButton.Button1 : MessageBoxDefaultButton.Button2);

			Interlocked.Decrement(ref g_nCurrentMessageCount);
			return (dr == DialogResult.Yes);
		}

		public static bool AskYesNo(string strText, string strTitle, bool bDefaultToYes)
		{
			return AskYesNo(strText, strTitle, bDefaultToYes, g_mbiQuestion);
		}

		public static bool AskYesNo(string strText, string strTitle)
		{
			return AskYesNo(strText, strTitle, true, g_mbiQuestion);
		}

		public static bool AskYesNo(string strText)
		{
			return AskYesNo(strText, null, true, g_mbiQuestion);
		}

		public static void ShowLoadWarning(string strFilePath, Exception ex)
		{
			ShowLoadWarning(strFilePath, ex, PwDefs.DebugMode);
		}

		public static void ShowLoadWarning(string strFilePath, Exception ex,
			bool bFullException)
		{
			ShowWarning(GetLoadWarningMessage(strFilePath, ex, bFullException));
		}

		public static void ShowLoadWarning(IOConnectionInfo ioc, Exception ex)
		{
			if (ioc != null) ShowLoadWarning(ioc.GetDisplayName(), ex);
			else ShowWarning(ex);
		}

		public static void ShowSaveWarning(string strFilePath, Exception ex,
			bool bCorruptionWarning)
		{
			FileLockException exFL = (ex as FileLockException);
			if (exFL != null) { ShowWarning(exFL); return; }

			ShowWarning(GetSaveWarningMessage(strFilePath, ex, bCorruptionWarning));
		}

		public static void ShowSaveWarning(IOConnectionInfo ioc, Exception ex,
			bool bCorruptionWarning)
		{
			if (ioc != null)
				ShowSaveWarning(ioc.GetDisplayName(), ex, bCorruptionWarning);
			else ShowWarning(ex);
		}
#endif // !KeePassUAP

		internal static string GetLoadWarningMessage(string strFilePath,
			Exception ex, bool bFullException)
		{
			object[] v = new object[] { strFilePath, KLRes.FileLoadFailed, ex };

			return ObjectsToMessage(v, bFullException);
		}

		internal static string GetSaveWarningMessage(string strFilePath,
			Exception ex, bool bCorruptionWarning)
		{
			object[] v = new object[] { strFilePath, KLRes.FileSaveFailed, ex,
				(bCorruptionWarning ? KLRes.FileSaveCorruptionWarning : null) };

			return ObjectsToMessage(v);
		}

		public static void ExternalIncrementMessageCount()
		{
			Interlocked.Increment(ref g_nCurrentMessageCount);
		}

		public static void ExternalDecrementMessageCount()
		{
			Interlocked.Decrement(ref g_nCurrentMessageCount);
		}
	}
}
