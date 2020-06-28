/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections;

namespace Belot
{
	/// <summary>
	/// Collection of cards.
	/// </summary>
	public class CardsCollection : ReadOnlyCollectionBase
	{
		public CardsCollection()
		{			
		}

		#region Indexer
		/// <summary>
		/// Indexer for the collection
		/// </summary>
		public Card this[ int index ]  
		{
			get  
			{
				return( (Card) InnerList[index] );
			}			
		}
		#endregion

		#region Methods
		internal virtual int Add( Card value )  
		{
			int ret =  InnerList.Add( value );			
			RaiseChanged();
			return ret;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="value"></param>
		/// <returns></returns>
		public virtual int IndexOf( Card value )  
		{
			return( InnerList.IndexOf( value ) );
		}

		internal virtual void Insert( int index, Card value )  
		{
			if ((index < 0) || (index > this.InnerList.Count))
			{
				throw new ArgumentOutOfRangeException("Card inserted on wrong place");
			}

			InnerList.Insert( index, value );
			RaiseChanged( );
		}

		internal virtual void Remove( Card value )  
		{
			InnerList.Remove( value );
			RaiseChanged( );
		}

		internal virtual void RemoveAt( int index )  
		{
			InnerList.RemoveAt( index );
			RaiseChanged( );
		}

		internal virtual void Clear()  
		{
			InnerList.Clear();
			RaiseChanged( );
		}

		/// <summary>
		/// Whether the collection contains a specific card.
		/// </summary>
		public bool Contains( Card value )  
		{
			// If value is not of type Card, this will return false.
			return( InnerList.Contains( value ) );
		}		

		internal void Sort( CardComparer comparer )
		{
			this.InnerList.Sort( comparer );
			this.InnerList.Reverse();
			RaiseChanged( );
		}
		#endregion	

		#region Events
		/// <summary>
		/// Handler delegate for the Changed event.
		/// </summary>
		public delegate void CardsCollectionChangedHandler ( );
				
		/// <summary>
		/// Occurs when collection has changed.
		/// </summary>
		public event CardsCollectionChangedHandler Changed;
			
		/// <summary>
		/// Raises the Changed event
		/// </summary>
		protected void RaiseChanged( )
		{
			if( Changed != null )
			{
				Changed( );
			}			
		}
		#endregion

		#region Properties
		/// <summary>
		/// Gets whether the collection is empty.
		/// </summary>
		public bool IsEmpty
		{
			get
			{
				return (InnerList.Count == 0);
			}
		}
		#endregion


	}
}
