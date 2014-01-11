/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System.Windows.Forms;

namespace SharpBelot
{
	class TransparentPanel : Panel
	{
		public TransparentPanel()
			: base()
		{
			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.Opaque, false );
			this.SetStyle( ControlStyles.SupportsTransparentBackColor, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.Selectable, false );
		}
	}
}
