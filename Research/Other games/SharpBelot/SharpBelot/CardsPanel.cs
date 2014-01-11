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
	public partial class CardsPanel : UserControl
	{
		#region Fields

		private const int ANGLE = 10;
		private const int TRANSFORM_X = -5;
		private const int TRANSFORM_Y = -8;

		private float _initialAngle;
		private float _initialX;
		private float _initialY;
		private Region[] _regions;
		private PlayerPosition _pos = PlayerPosition.South;
		private CardsCollection _cards;
		private int _cardsCount;
		private bool _areBacks;

		#endregion

		public event CardClicked CardClick;

		public CardsPanel()
		{
			InitializeComponent();

			this.SetStyle( ControlStyles.AllPaintingInWmPaint, true );
			this.SetStyle( ControlStyles.DoubleBuffer, true );
			this.SetStyle( ControlStyles.Opaque, false );
			this.SetStyle( ControlStyles.UserMouse, true );
			this.SetStyle( ControlStyles.ContainerControl, false );
			this.SetStyle( ControlStyles.SupportsTransparentBackColor, true );
			this.SetStyle( ControlStyles.UserPaint, true );
			this.SetStyle( ControlStyles.Selectable, false );
		}

		#region Properties

		[Browsable( true )]
		public PlayerPosition TablePosition
		{
			get
			{
				return _pos;
			}
			set
			{
				_pos = value;
			}
		}

		[Browsable( false ),
			DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
		public CardsCollection Cards
		{
			get
			{
				return _cards;
			}
			set
			{
				_cards = value;
				_cardsCount = _cards.Count;
				_regions = new Region[ _cardsCount ];
				for ( int i = 0; i < _cardsCount; i++ )
				{
					_regions[ i ] = new Region();
				}
				Invalidate();
			}
		}

		[Browsable( false ),
			DesignerSerializationVisibility( DesignerSerializationVisibility.Hidden )]
		public bool AreBacks
		{
			get
			{
				return _areBacks;
			}
			set
			{
				_areBacks = value;
			}
		}

		#endregion

		#region Draw methods

		protected override void OnPaint( PaintEventArgs e )
		{
			if ( !this.DesignMode )
			{
				init();

				draw( e.Graphics );
			}
		}

		private void draw( Graphics g )
		{
			g.TranslateTransform( _initialX, _initialY );
			g.RotateTransform( _initialAngle );

			for ( int i = 0; i < _cardsCount; i++ )
			{
				Image im;
				if ( _areBacks )
				{
					im = CardGUIProvider.GetBack();
				}
				else
				{
					im = CardGUIProvider.GetCardImage( _cards[ i ] );
				}

				_regions[ i ] = new Region( new Rectangle( 0, 0, CardGUIProvider.ImageWidth, CardGUIProvider.ImageHeight ) );
				_regions[ i ].Transform( g.Transform );

				g.DrawImage( im, 0, 0 );
				g.TranslateTransform( TRANSFORM_X, TRANSFORM_Y );
				g.RotateTransform( ANGLE );
			}

			for ( int i = 0; i < _cardsCount-1; i++ )
			{
				_regions[ i ].Exclude( _regions[ i+1 ] );
			}
		}

		private void init()
		{
			switch ( _pos )
			{
				case PlayerPosition.South:
					_initialAngle = 180 - ( ( _cardsCount-1 ) / 2F ) * ANGLE;
					_initialX = ( this.Width + CardGUIProvider.ImageWidth )/2 + _cardsCount * TRANSFORM_X / 2;
					_initialY = ( this.Height + CardGUIProvider.ImageHeight )/2 + _cardsCount * TRANSFORM_Y / 2;
					break;
				case PlayerPosition.East:
					_initialAngle = 90 - ( ( _cardsCount-1 ) / 2F ) * ANGLE;
					_initialX = ( this.Width + CardGUIProvider.ImageHeight )/2 + _cardsCount * TRANSFORM_Y / 2;
					_initialY = ( this.Height - CardGUIProvider.ImageWidth )/2 - _cardsCount * TRANSFORM_X / 2;
					break;
				case PlayerPosition.North:
					_initialAngle = 0 - ( ( _cardsCount-1 ) / 2F ) * ANGLE;
					_initialX = ( this.Width - CardGUIProvider.ImageWidth )/2 - _cardsCount * TRANSFORM_X / 2;
					_initialY = ( this.Height - CardGUIProvider.ImageHeight )/2 - _cardsCount * TRANSFORM_Y / 2;
					break;
				case PlayerPosition.West:
					_initialAngle = 270 - ( ( _cardsCount-1 ) / 2F ) * ANGLE;
					_initialX = ( this.Width - CardGUIProvider.ImageHeight )/2 - _cardsCount * TRANSFORM_Y / 2;
					_initialY = ( this.Height + CardGUIProvider.ImageWidth )/2 + _cardsCount * TRANSFORM_X / 2;
					break;
			}
		}

		#endregion

		#region Overrides

		protected override void OnMouseDown( MouseEventArgs e )
		{
			for ( int i = 0; i < _cardsCount; i++ )
			{
				if ( _regions[ i ].IsVisible( e.X, e.Y ) )
				{
					if ( CardClick != null && _cards[ i ].IsSelectable )
					{
						CardClick( this, new CardClickedEventArgs( _cards[ i ] ) );
					}
				}
			}
		}

		protected override void OnMouseMove( MouseEventArgs e )
		{
			bool onCard = false;
			for ( int i = 0; i < _cardsCount; i++ )
			{
				if ( _regions[ i ].IsVisible( e.X, e.Y ) )
				{
					onCard = true;
					if ( _cards[ i ].IsSelectable )
					{
						this.Cursor = Cursors.Hand;
					}
					else
					{
						this.Cursor = Cursors.No;
					}
				}
			}

			if ( !onCard )
			{
				this.Cursor = Cursors.Arrow;
			}
		}

		protected override void OnMouseLeave( EventArgs e )
		{
			this.Cursor = Cursors.Arrow;
		}


		#endregion
	}

	public delegate void CardClicked( CardsPanel panel, CardClickedEventArgs e );

	#region Class CardClickedEventArgs

	public class CardClickedEventArgs : EventArgs
	{
		private Card _card;
		public CardClickedEventArgs( Card card )
		{
			_card = card;
		}

		public Card Card
		{
			get
			{
				return _card;
			}
		}
	}

	#endregion
}
