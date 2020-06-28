/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using Belot;

namespace SharpBelot
{
	public partial class CardPictureBox : UserControl
	{
		private Card _card;
		private Image _image;

		public CardPictureBox()
		{
			InitializeComponent();

			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.Opaque, false );
			this.SetStyle( ControlStyles.SupportsTransparentBackColor, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.Selectable, false );

			this.Width = CardGUIProvider.ImageWidth;
			this.Height = CardGUIProvider.ImageHeight;
		}

		#region Properties

		[Browsable( false ),
			DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
		public Card Card
		{
			get
			{
				return _card;
			}
			set
			{
				_card = value;

				_image = CardGUIProvider.GetCardImage( _card );

				this.Invalidate();
				this.Update();
			}
		}

		#endregion

		protected override void OnPaint( PaintEventArgs e )
		{
			if ( _image != null )
			{
				e.Graphics.DrawImage( _image, 0, 0, this.Width, this.Height );
			}
		}
	}
}
