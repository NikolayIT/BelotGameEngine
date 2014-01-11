/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using Belot;

namespace SharpBelot
{
	public partial class DealResultForm : Form
	{
		private readonly int IMAGE_WIDTH;
		private readonly int IMAGE_HEIGTH;
		private const int PAD_WIDTH = 20;
		private const int PAD_HEIGTH = 20;
		private const int LABEL_HEIGHT = 25;

		private IDictionary<CardCombination, Player> _mapCombinationToPlayer;
		private IList<Hand> _hands;
		private Font _labelFont = null;
		private Belot.Deal _deal;

		public DealResultForm()
		{
			InitializeComponent();

			_mapCombinationToPlayer = new Dictionary<CardCombination, Player>();
			_hands = new List<Hand>();

			_labelFont = Properties.Settings.Default.MistralFont;

			IMAGE_WIDTH = CardGUIProvider.ImageWidth;
			IMAGE_HEIGTH = CardGUIProvider.ImageHeight;
		}

		#region Public Methods

		public void AddHand( Hand hand )
		{
			_hands.Add( hand );
		}

		public void AddCombination( Player player, CardCombination combination )
		{
			_mapCombinationToPlayer.Add( combination, player );
		}

		public void ChangeResources()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager( this.GetType() );
			resources.ApplyResources( this._buttonOK, "_buttonOK" );
			resources.ApplyResources( this._groupBoxWE, "_groupBoxWE" );
			resources.ApplyResources( this._labelWEPointsReal, "_labelWEPointsReal" );
			resources.ApplyResources( this._labelWEPoints, "_labelWEPoints" );
			resources.ApplyResources( this._labelWECombinations, "_labelWECombinations" );
			resources.ApplyResources( this._panelWECombinations, "_panelWECombinations" );
			resources.ApplyResources( this._labelWEHands, "_labelWEHands" );
			resources.ApplyResources( this._panelWEHands, "_panelWEHands" );
			resources.ApplyResources( this._groupBoxSN, "_groupBoxSN" );
			resources.ApplyResources( this._labelSNPointsReal, "_labelSNPointsReal" );
			resources.ApplyResources( this._labelSNPoints, "_labelSNPoints" );
			resources.ApplyResources( this._labelSNCombinations, "_labelSNCombinations" );
			resources.ApplyResources( this._panelSNCombinations, "_panelSNCombinations" );
			resources.ApplyResources( this._labelSNHands, "_labelSNHands" );
			resources.ApplyResources( this._panelSNHands, "_panelSNHands" );
			resources.ApplyResources( this, "$this" );
		}

		#endregion

		public Belot.Deal Deal
		{
			set
			{
				_deal = value;
			}
		}

		#region Private Methods

		private void ButtonOKClick( object sender, EventArgs e )
		{
			_mapCombinationToPlayer.Clear();
			_hands.Clear();
			_panelSNHands.Controls.Clear();
			_panelWEHands.Controls.Clear();
			_panelSNCombinations.Controls.Clear();
			_panelWECombinations.Controls.Clear();
			this.DialogResult = DialogResult.OK;
		}

		private string GetCombinationString( CardCombination combination )
		{
			StringBuilder sb = new StringBuilder();

			foreach ( Card card in combination.Cards )
			{
				sb.Append( card.ToString() + " " );
			}

			return sb.ToString();
		}

		private void OnVisibleChanged( object sender, EventArgs e )
		{
			if ( this.Visible )
			{
				#region _hands

				int rowsSN = 0;
				int rowsWE = 0;

				foreach ( Hand hand in _hands )
				{
					int cols = 0;
					foreach ( Card card in hand )
					{
						if ( hand.Winner.Position == PlayerPosition.South || hand.Winner.Position == PlayerPosition.North )
						{
							CardPictureBox cpb = new CardPictureBox();
							cpb.Width = IMAGE_WIDTH;
							cpb.Height = IMAGE_HEIGTH;
							cpb.Card = card;
							cpb.Location = new Point( cols * ( IMAGE_WIDTH + PAD_WIDTH ) + PAD_WIDTH, rowsSN * ( IMAGE_HEIGTH + PAD_HEIGTH ) + PAD_HEIGTH );

							_panelSNHands.Controls.Add( cpb );
						}
						else
						{
							CardPictureBox cpb = new CardPictureBox();
							cpb.Width = IMAGE_WIDTH;
							cpb.Height = IMAGE_HEIGTH;
							cpb.Card = card;
							cpb.Location = new Point( cols * ( IMAGE_WIDTH + PAD_WIDTH ) + PAD_WIDTH, rowsWE * ( IMAGE_HEIGTH + PAD_HEIGTH ) + PAD_HEIGTH );

							_panelWEHands.Controls.Add( cpb );
						}
						cols++;
					}

					if ( hand.Winner.Position == PlayerPosition.South || hand.Winner.Position == PlayerPosition.North )
					{
						rowsSN++;
					}
					else
					{
						rowsWE++;
					}
				}

				#endregion

				#region Combinations

				int lastSNlabelTop = 0;
				int lastWElabelTop = 0;

				foreach ( KeyValuePair<CardCombination, Player> kv in _mapCombinationToPlayer )
				{
					CardCombination combination = kv.Key;
					Player player = kv.Value;

					Label labelCombination = new Label();
					labelCombination.Text = GetCombinationString( combination );
					labelCombination.Width = 160;
					labelCombination.Height = LABEL_HEIGHT;
					labelCombination.Font = _labelFont;

					Label labelPoints = new Label();
					labelPoints.Text = combination.Points.ToString();
					labelPoints.Width = 40;
					labelPoints.Height = LABEL_HEIGHT;
					labelPoints.Font = _labelFont;

					PictureBox pictureCounted = new PictureBox();
					pictureCounted.Width = LABEL_HEIGHT;
					pictureCounted.Height = LABEL_HEIGHT;
					pictureCounted.SizeMode = PictureBoxSizeMode.StretchImage;
					if ( combination.IsCounted )
					{
						pictureCounted.Image = Properties.Resources.yes;
					}
					else
					{
						pictureCounted.Image = Properties.Resources.no;
					}

					if ( player.Position == PlayerPosition.South || player.Position == PlayerPosition.North )
					{
						labelCombination.Location = new Point( 0, lastSNlabelTop );
						labelPoints.Location = new Point( 160, lastSNlabelTop );
						pictureCounted.Location = new Point( 200, lastSNlabelTop );

						_panelSNCombinations.Controls.Add( labelCombination );
						_panelSNCombinations.Controls.Add( labelPoints );
						_panelSNCombinations.Controls.Add( pictureCounted );
						lastSNlabelTop += LABEL_HEIGHT;
					}
					else
					{
						labelCombination.Location = new Point( 0, lastWElabelTop );
						labelPoints.Location = new Point( 160, lastWElabelTop );
						pictureCounted.Location = new Point( 200, lastWElabelTop );

						_panelWECombinations.Controls.Add( labelCombination );
						_panelWECombinations.Controls.Add( labelPoints );
						_panelWECombinations.Controls.Add( pictureCounted );
						lastWElabelTop += LABEL_HEIGHT;
					}
				}

				#endregion

				_labelSNPointsReal.Text = _deal.RawNorthSouthPoints.ToString();
				_labelWEPointsReal.Text = _deal.RawEastWestPoints.ToString();
			}
		}

		#endregion
	}
}