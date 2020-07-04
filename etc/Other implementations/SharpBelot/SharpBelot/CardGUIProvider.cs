/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Text;
using Belot;

namespace SharpBelot
{
	class CardGUIProvider
	{
		private static System.Windows.Forms.ImageList _imageList;
		private static Image _back;
		private static int IMAGE_WIDTH = 72;
		private static int IMAGE_HEIGTH = 96;

		static CardGUIProvider()
		{
			_imageList = new System.Windows.Forms.ImageList();
		}

		public static void Reload()
		{
			_imageList.Images.Clear();
			Load();
		}

		public static Image GetBack()
		{
			return _back;
		}

		public static Image GetCardImage( Card card )
		{
			int index = 0;

			switch ( card.CardType )
			{
				case CardType.Ace:
					index = 0;
					break;
				case CardType.King:
					index = 1;
					break;
				case CardType.Queen:
					index = 2;
					break;
				case CardType.Jack:
					index = 3;
					break;
				case CardType.Ten:
					index = 4;
					break;
				case CardType.Nine:
					index = 5;
					break;
				case CardType.Eight:
					index = 6;
					break;
				case CardType.Seven:
					index = 7;
					break;
			}

			index *= 4;

			switch ( card.CardColor )
			{
				case CardColor.Spades:
					index += 0;
					break;
				case CardColor.Hearts:
					index += 1;
					break;
				case CardColor.Diamonds:
					index += 2;
					break;
				case CardColor.Clubs:
					index += 3;
					break;
			}

			return _imageList.Images[ index ];
		}

		private static void Load()
		{
			if ( !File.Exists( Properties.Settings.Default.CardsDll ) )
			{
				throw new FileNotFoundException( Properties.Settings.Default.CardsDll );
			}

			Assembly picsAssembly = Assembly.LoadFrom( Properties.Settings.Default.CardsDll );
			_imageList.ImageSize = new Size( IMAGE_WIDTH, IMAGE_HEIGTH );
			Type res = picsAssembly.GetType( "CardRes.Properties.Resources" );
			Type imType = typeof( Bitmap );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "AS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "AH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "AD", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "AC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "KS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "KH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "KD", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "KC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "QS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "QH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "QD", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "QC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "JS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "JH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "JD", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "JC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "TS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "TH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "TD", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "TC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "NS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "NH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "ND", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "NC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "ES", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "EH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "ED", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "EC", imType ).GetValue( null, null ) ) );

			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "SS", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "SH", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "SD", imType ).GetValue( null, null ) ) );
			_imageList.Images.Add( RemoveCorners( ( Image )res.GetProperty( "SC", imType ).GetValue( null, null ) ) );

			_back = RemoveCorners( ( Image )res.GetProperty( "Back", imType ).GetValue( null, null ) );

		}

		private static Image RemoveCorners( Image im )
		{
			Bitmap b = new Bitmap( im );
			b.SetPixel( 0, 0, Color.Transparent );
			b.SetPixel( 1, 0, Color.Transparent );
			b.SetPixel( 0, 1, Color.Transparent );

			b.SetPixel( b.Width - 1, 0, Color.Transparent );
			b.SetPixel( b.Width - 2, 0, Color.Transparent );
			b.SetPixel( b.Width - 1, 1, Color.Transparent );

			b.SetPixel( 0, b.Height - 1, Color.Transparent );
			b.SetPixel( 0, b.Height - 2, Color.Transparent );
			b.SetPixel( 1, b.Height - 1, Color.Transparent );

			b.SetPixel( b.Width - 1, b.Height - 1, Color.Transparent );
			b.SetPixel( b.Width - 1, b.Height - 2, Color.Transparent );
			b.SetPixel( b.Width - 2, b.Height - 1, Color.Transparent );

			return b;
		}

		public static int ImageWidth
		{
			get
			{
				return IMAGE_WIDTH;
			}
		}

		public static int ImageHeight
		{
			get
			{
				return IMAGE_HEIGTH;
			}
		}
	}
}
