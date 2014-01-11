/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;

namespace Belot
{
	/// <summary>
	/// The announcement ( bidding ) in the current deal.
	/// </summary>
	public class Announcement : IComparable
	{
		#region Fields

		private AnnouncementTypeEnum _type;
		private bool _isDoubled;
		private bool _isReDoubled;

		#endregion

		#region Constructor

		/// <summary>
		/// Creates a new announcement
		/// </summary>
		public Announcement( AnnouncementTypeEnum type, bool isDoubled, bool isReDoubled )
		{
			_type = type;
			_isDoubled = isDoubled;
			_isReDoubled = isReDoubled;
		}

		#endregion

		#region Properties

		/// <summary>
		/// The bid the current Announcement represents. (All Trumps, Pass, etc.) 
		/// </summary>
		public AnnouncementTypeEnum Type
		{
			get { return _type; }
		}

		/// <summary>
		/// Rerurns if current game is doubled.
		/// </summary>
		public bool IsDoubled
		{
			get { return _isDoubled; }
		}

		/// <summary>
		/// Rerurns if current game is re-doubled.
		/// </summary>
		public bool IsReDoubled
		{
			get { return _isReDoubled; }
		}

		#endregion

		#region IComparable Members

		/// <summary>
		/// Compares the current Announcement to an object of type AnnouncementTypeEnum or Announcement.
		/// </summary>
		/// <param name="obj">Object of type AnnouncementTypeEnum or Announcement to compare to.</param>
		/// <returns>1 if current announce (bid) is bigger, -1 if is not bigger, 0 if both are equal</returns>
		public int CompareTo( object obj )
		{
			if( obj is AnnouncementTypeEnum )
			{
				return CompareTo( ( ( AnnouncementTypeEnum )obj ) );
			}
			else if( obj is Announcement )
			{
				return CompareTo( ( ( Announcement )obj ).Type );
			}
			else
			{
				throw new ArgumentException( "Announcement can be compared only to objects of type AnnouncementTypeEnum or Announcement." );
			}
		}

		private int CompareTo( AnnouncementTypeEnum type )
		{
			int x = 0, y = 0;

			switch( this._type )
			{
				case AnnouncementTypeEnum.AllTrumps:
					x = 7;
					break;
				case AnnouncementTypeEnum.NoTrumps:
					x = 6;
					break;
				case AnnouncementTypeEnum.Spades:
					x = 5;
					break;
				case AnnouncementTypeEnum.Hearts:
					x = 4;
					break;
				case AnnouncementTypeEnum.Diamonds:
					x = 3;
					break;
				case AnnouncementTypeEnum.Clubs:
					x = 2;
					break;
				case AnnouncementTypeEnum.Pass:
					x = 1;
					break;
			}

			switch( type )
			{
				case AnnouncementTypeEnum.AllTrumps:
					y = 7;
					break;
				case AnnouncementTypeEnum.NoTrumps:
					y = 6;
					break;
				case AnnouncementTypeEnum.Spades:
					y = 5;
					break;
				case AnnouncementTypeEnum.Hearts:
					y = 4;
					break;
				case AnnouncementTypeEnum.Diamonds:
					y = 3;
					break;
				case AnnouncementTypeEnum.Clubs:
					y = 2;
					break;
				case AnnouncementTypeEnum.Pass:
					y = 1;
					break;
			}

			if( x > y )
				return 1;
			else if( x < y )
				return -1;
			else
				return 0;
		}

		#endregion	
		
	}
}