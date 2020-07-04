/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.Text;
using Belot;

namespace SharpBelot
{
	/// <summary>
	/// Represents a human player.
	/// </summary>
	class HumanPlayer : Player
	{
		#region Constructors

		/// <summary>
		/// Constructor for the class. Creates a new human player with the specified name.
		/// </summary>
		/// <param name="name">The name of the player</param>
		public HumanPlayer( string name )
			: base( name )
		{
		}
		#endregion

		#region AnnounceMaking Event
		/// <summary>
		/// Handler delegate for the AnnounceMaking event.
		/// </summary>
		public delegate Announcement AnnounceMakingHandler( Player player, AnnouncementManager manager );

		/// <summary>
		/// Occurs when the player is to make an announce.
		/// </summary>
		public event AnnounceMakingHandler AnnounceMaking;

		/// <summary>
		/// Raises the AnnounceMaking event
		/// </summary>
		protected Announcement RaiseAnnounceMaking( AnnouncementManager manager )
		{
			if ( AnnounceMaking != null )
			{
				return AnnounceMaking( this, manager );
			}
			else
			{
				return null;
			}
		}
		#endregion

		#region CardCombinationAnnouncing Event
		/// <summary>
		/// Handler delegate for the CardCombinationAnnouncing event.
		/// </summary>
		public delegate bool CardCombinationAnnouncingHandler( Player player, CardCombination combination );

		/// <summary>
		/// Occurs when the player has to announce a combination of in his cards.
		/// </summary>
		public event CardCombinationAnnouncingHandler CardCombinationAnnouncing;

		/// <summary>
		/// Raises the CardCombinationAnnouncing event
		/// </summary>
		protected bool RaiseCardCombinationAnnouncing( CardCombination combination )
		{
			if ( CardCombinationAnnouncing != null )
			{
				return CardCombinationAnnouncing( this, combination );
			}
			else
			{
				return false;
			}
		}
		#endregion

		#region Overriden Base Methods

		public override Announcement MakeAnnouncement( AnnouncementManager manager )
		{
			Announcement ret = RaiseAnnounceMaking( manager );

			return ret;
		}

		public override bool AnnounceCombination( CardCombination combination )
		{
			return RaiseCardCombinationAnnouncing( combination );
		}

		protected override void PlayCard( PlayingManager manager )
		{
			RaiseCardPlaying( manager );
		}

		#endregion

		public void PlayCard( Card card )
		{
			RaiseCardPlayed( card );
		}
	}
}
