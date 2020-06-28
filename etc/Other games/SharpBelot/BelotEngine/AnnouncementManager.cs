/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections;
using System.Collections.Generic;

namespace Belot
{
	/// <summary>
	/// A manager that observes bidding rules.
	/// </summary>
	public class AnnouncementManager
	{
		#region Fields
		private IList< Announcement > _announces;
		private IList< Player > _players;

		private Announcement _lastValidAnnouncement;
		private Player _lastBidder;

		private bool _isBiddingFinished; 

		#endregion

		#region Constructor
		/// <summary>
		/// Constructor for the class
		/// </summary>
		internal AnnouncementManager()
		{
			_announces = new List< Announcement >();
			_players = new List< Player >();

			_isBiddingFinished = false;

			_lastValidAnnouncement = new Announcement( AnnouncementTypeEnum.Pass, false, false );
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets a value whether bidding is finished. (4 successive passes)
		/// </summary>
		internal bool IsBiddingFinished
		{
			get { return _isBiddingFinished; }
		}
		#endregion

		#region Private and Internal Methods
		/// <summary>
		/// Stores and announcement made by a player
		/// </summary>
		/// <param name="player"></param>
		/// <param name="announce"></param>
		internal void Add( Player player, Announcement announce )  
		{
			
			if( !IsValid( player, announce ))
				throw new InvalidOperationException( "You cannot bid lower than current" );

			if( (_players.Count != 0)&&( player == (Player)_players[_players.Count - 1] ) )
				throw new InvalidOperationException( "You cannot bid twice" );
			
			_announces.Add( announce );
			_players.Add( player );

			if( announce.Type != AnnouncementTypeEnum.Pass )
			{
				_lastValidAnnouncement = announce;
				_lastBidder = player;
			}
			else
			{
				//4 successive passes end bidding or 3 passes after a legal bid				
				if( _announces.Count > 3 )
				{
					if( ( ( _announces[_announces.Count-1]).Type == AnnouncementTypeEnum.Pass ) &&
						( ( _announces[_announces.Count-2]).Type == AnnouncementTypeEnum.Pass ) &&
						( ( _announces[_announces.Count-3]).Type == AnnouncementTypeEnum.Pass ) &&
						( _lastBidder != null || (_announces[_announces.Count-4]).Type == AnnouncementTypeEnum.Pass ) )
					{
						this._isBiddingFinished = true;
					}
				}
			}
		}

		/// <summary>
		/// Gets the player who made the last valid announcement
		/// </summary>
		/// <returns></returns>
		internal Player GetLastBidder( )
		{			
			return _lastBidder;
		}		

		private bool IsLastValidBidByTeam( Player player )
		{
			if( _lastBidder == player )
				return true;

			if( _players.Count >= 2 )
			{
				if( _players[_players.Count-2] == _lastBidder )
					return true;
			}

			return false;
		}
		#endregion

		#region Public Methods
		/// <summary>
		/// Gets the last valid announcement
		/// </summary>
		/// <returns></returns>
		public Announcement GetLastValidAnnouncement( )
		{			
			return _lastValidAnnouncement;
		}		
		
		/// <summary>
		/// Gets all announces in current deal
		/// </summary>
		/// <returns>list of (announce-player) pairs</returns>
		public IDictionary<Announcement, Player> GetAllAnnouncements()
		{
			IDictionary< Announcement, Player > map = new Dictionary< Announcement, Player >();
			for( int i = 0; i< _announces.Count; i++ )
			{
				map.Add( _announces[i], _players[i] );
			}

			return map;
		}

		/// <summary>
		/// If announcement made by this player is valid according to game rules
		/// </summary>
		public bool IsValid( Player player, Announcement announce )
		{			
			if( player == null)
				throw new ArgumentNullException( "Player", "Bidding player cannot be null");
				
			if(	announce == null)
				throw new ArgumentNullException( "Announcement", "Announcement cannot be null");
			
			return IsValid( player, announce.Type, announce.IsDoubled, announce.IsReDoubled );			
		}
		
		/// <summary>
		/// If announcement made by this player is valid according to game rules
		/// </summary>
		public bool IsValid( Player player, AnnouncementTypeEnum type, bool isDoubled, bool isRedoubled )
		{
			//no player
			if( player == null)
				throw new ArgumentNullException( "Player", "Bidding player cannot be null");
			
			//legal pass
			if( type == AnnouncementTypeEnum.Pass )
			{
				if( !isDoubled && !isRedoubled )
					return true;
				else
					return false;
			}
			
			if( _lastValidAnnouncement.CompareTo( type ) < 0  && !isDoubled && !isRedoubled )
				return true;
			
			if( _lastValidAnnouncement.CompareTo( type ) == 0 && !IsLastValidBidByTeam( player ) && !_lastValidAnnouncement.IsReDoubled)
			{
				if( !_lastValidAnnouncement.IsDoubled && isDoubled && !isRedoubled )
					return true;
				if( _lastValidAnnouncement.IsDoubled && !isDoubled && isRedoubled )
					return true;
			}
			
			return false;
		}


		#endregion
	}
}
