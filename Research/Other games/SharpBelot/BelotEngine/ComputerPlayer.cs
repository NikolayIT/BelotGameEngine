/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

namespace Belot
{
	/// <summary>
	/// Represents a computer player.
	/// </summary>
	public abstract class ComputerPlayer : Player
	{
		#region Constructors

		/// <summary>
		/// Default constructor
		/// </summary>
		protected ComputerPlayer()
		{			
		}

		/// <summary>
		/// Constructor for the class. Creates a new computer player with the specified name.
		/// </summary>
		/// <param name="name">The name of the player</param>
		protected ComputerPlayer( string name ): base( name )
		{				
		}
		#endregion

		#region AnnounceMade Event
		/// <summary>
		/// Handler delegate for the AnnounceMade event.
		/// </summary>
		public delegate void AnnounceMadeHandler ( Player player, Announcement announce );
			
		/// <summary>
		/// Occurs when the player has made an announce.
		/// </summary>
		public event AnnounceMadeHandler AnnounceMade;
		
		/// <summary>
		/// Raises the AnnounceMade event
		/// </summary>
		protected void RaiseAnnounceMade( Announcement announce )
		{
			if( AnnounceMade != null )
			{
				AnnounceMade( this, announce );
			}			
		}
		#endregion
		
		#region CombinationAnnounced Event
		/// <summary>
		/// Handler delegate for the CardCombinationAnnounced event.
		/// </summary>
		public delegate void CardCombinationAnnouncedHandler ( Player player, CardCombination combination );
			
		/// <summary>
		/// Occurs when player has announced a combination.
		/// </summary>
		public event CardCombinationAnnouncedHandler CardCombinationAnnounced;
		
		/// <summary>
		/// Raises the CombinationAnnounced event
		/// </summary>
		protected void RaiseCardCombinationAnnounced( CardCombination combination )
		{
			if( CardCombinationAnnounced != null )
			{
				CardCombinationAnnounced( this, combination );
			}			
		}
		#endregion
		
	}
}
