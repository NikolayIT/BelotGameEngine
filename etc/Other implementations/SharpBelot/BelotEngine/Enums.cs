/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

namespace Belot
{
	#region PlayerPosition
	/// <summary>
	/// Players positions on the table.
	/// </summary>
	public enum PlayerPosition
	{
		/// <summary>
		/// 
		/// </summary>
		South,
		/// <summary>
		/// 
		/// </summary>
		East,
		/// <summary>
		/// 
		/// </summary>
		North,
		/// <summary>
		/// 
		/// </summary>
		West
	}
	#endregion

	#region CardColor
	/// <summary>
	/// Cards colors
	/// </summary>
	public enum CardColor
	{
		/// <summary>
		/// 
		/// </summary>
		Spades,
		/// <summary>
		/// 
		/// </summary>
		Hearts,
		/// <summary>
		/// 
		/// </summary>
		Diamonds,
		/// <summary>
		/// 
		/// </summary>
		Clubs
	}
	#endregion

	#region CardType
	/// <summary>
	/// Card types
	/// </summary>
	public enum CardType
	{
		/// <summary>
		/// 
		/// </summary>
		Ace,
		/// <summary>
		/// 
		/// </summary>
		King,
		/// <summary>
		/// 
		/// </summary>
		Queen,
		/// <summary>
		/// 
		/// </summary>
		Jack,
		/// <summary>
		/// 
		/// </summary>
		Ten,
		/// <summary>
		/// 
		/// </summary>
		Nine,
		/// <summary>
		/// 
		/// </summary>
		Eight,
		/// <summary>
		/// 
		/// </summary>
		Seven
	}
	#endregion

	#region AnnouncementTypeEnum
	/// <summary>
	/// Types of Belot games.
	/// </summary>
	public enum AnnouncementTypeEnum
	{
		/// <summary>
		/// 
		/// </summary>
		Pass,
		/// <summary>
		/// 
		/// </summary>
		AllTrumps,
		/// <summary>
		/// 
		/// </summary>
		NoTrumps,
		/// <summary>
		/// 
		/// </summary>
		Spades,
		/// <summary>
		/// 
		/// </summary>
		Hearts,
		/// <summary>
		/// 
		/// </summary>
		Diamonds,
		/// <summary>
		/// 
		/// </summary>
		Clubs
	}
	#endregion
}
