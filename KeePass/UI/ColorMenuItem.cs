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

using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace KeePass.UI
{
	public sealed class ColorMenuItem : ToolStripMenuItem
	{
		private readonly Color m_clr;
		private readonly int m_qSize;

		public Color Color
		{
			get { return m_clr; }
		}

		public ColorMenuItem(Color clr, int qSize)
		{
			m_clr = clr;
			m_qSize = qSize;

			Debug.Assert(this.CanRaiseEvents);
			this.ShowShortcutKeys = false;

			if(AccessibilityEx.Enabled)
				this.Text = UIUtil.ColorToString(clr);
		}

		protected override void OnPaint(PaintEventArgs e)
		{
			// base.OnDrawItem(e);

			Graphics g = e.Graphics;
			RectangleF rectBounds = e.Graphics.ClipBounds;
			RectangleF rectFill = new RectangleF(rectBounds.Left + 2,
				rectBounds.Top + 2, rectBounds.Width - 4, rectBounds.Height - 4);

			bool bFocused = e.Graphics.IsVisible(rectFill) && this.Selected;

			// e.DrawBackground();
			// e.DrawFocusRectangle();
			using(SolidBrush sbBack = new SolidBrush(bFocused ?
				SystemColors.Highlight : SystemColors.Menu))
			{
				g.FillRectangle(sbBack, rectBounds);
			}

			using(SolidBrush sb = new SolidBrush(m_clr))
			{
				g.FillRectangle(sb, rectFill);
			}
		}


		protected override void SetBounds(Rectangle rect)
		{
			rect.Height = m_qSize;
			rect.Width = m_qSize;

			base.SetBounds(rect);
		}
	}
}
