/*
 * Author: Konstantin Ivanov
 * 
 * Official site: http://konstantini.data.bg/sharpbelot
 * 
 * */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Drawing;

namespace SharpBelot
{
	/// <summary>
	/// Handler delegate for the AnimationFinish event.
	/// </summary>

	public delegate void AnimationFinishHandler();


	class CardAnimator
	{
		private int STEP;

		private bool _horizontalMovementFinished = false;
		private bool _verticalMovementFinished = false;
		private Timer _timer = null;
		private Point _destination;
		private Point _initialLocation;
		private Queue<CardPictureBox> _animationQueue;
		private CardPictureBox _animated = null;
		private CardPictureBox _target = null;

		public CardAnimator( CardPictureBox animated )
		{
			_animated = animated;
			_animationQueue = new Queue<CardPictureBox>( 4 );
			_timer = new Timer();
			_timer.Interval = 25;
			_timer.Tick += new System.EventHandler( OnTick );
		}

		/// <summary>
		/// Occurs when player cards have changed.
		/// </summary>
		public event AnimationFinishHandler AnimationFinished;

		public void Animate( Point destination )
		{
			_destination = destination;
			STEP = Properties.Settings.Default.Speed + 5;

			if ( _animationQueue.Count == 0 )
			{
				Finish();
			}
			else
			{
				InitNextControl();
				_timer.Start();
			}
		}

		public void EnqueueControl( CardPictureBox target )
		{
			_animationQueue.Enqueue( target );
		}

		private void InitNextControl()
		{
			_horizontalMovementFinished = false;
			_verticalMovementFinished = false;
			_target = ( CardPictureBox )_animationQueue.Dequeue();

			_animated.Left = _target.Left + _target.Parent.Left;
			_animated.Top = _target.Top + _target.Parent.Top;
			_animated.Card = _target.Card;
			_target.Visible = false;
			_target.Parent.SendToBack();
			_initialLocation = _target.Location;
			_animated.BringToFront();
			_animated.Visible = true;

		}

		private void OnTick( object sender, System.EventArgs e )
		{
			if ( !( _horizontalMovementFinished && _verticalMovementFinished ) )
			{
				if ( !_horizontalMovementFinished )
				{
					if ( _initialLocation.X < _destination.X )
					{
						if ( _animated.Left < _destination.X )
						{
							_animated.Left += STEP;
						}
						else
						{
							_animated.Left = _destination.X;
							_horizontalMovementFinished = true;
						}
					}
					else
					{
						if ( _animated.Left > _destination.X )
						{
							_animated.Left -= STEP;
						}
						else
						{
							_animated.Left = _destination.X;
							_horizontalMovementFinished = true;
						}
					}
				}

				if ( !_verticalMovementFinished )
				{
					if ( _initialLocation.Y < _destination.Y )
					{
						if ( _animated.Top < _destination.Y )
						{
							_animated.Top += STEP;
						}
						else
						{
							_animated.Top = _destination.Y;
							_verticalMovementFinished = true;
						}
					}
					else
					{
						if ( _animated.Top > _destination.Y )
						{
							_animated.Top -= STEP;
						}
						else
						{
							_animated.Top = _destination.Y;
							_verticalMovementFinished = true;
						}
					}
				}

				_animated.Invalidate();
			}
			else
			{
				if ( _animationQueue.Count == 0 )
				{
					Finish();
				}
				else
				{
					InitNextControl();
				}
			}
		}

		private void Finish()
		{
			_animated.Visible = false;
			_timer.Stop();

			if ( AnimationFinished != null )
			{
				AnimationFinished();
			}
		}
	}
}
