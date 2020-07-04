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
using System.Diagnostics;

namespace SharpBelot
{
	public partial class AboutForm : Form
	{
		private Image _logo1;
		private Image _logo2;
		private bool _diminish = false;
		private int _left = 0;
		private int _width = 0;
		private int _step = 5;

		public AboutForm()
		{
			InitializeComponent();

			_logo1 = Properties.Resources.QH;
			_logo2 = Properties.Resources.KH;

			_picture.Image = _logo1;
			_left = _picture.Left;
			_width = _logo1.Width;

			_timer.Start();
		}

		private void LinkClicked( object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e )
		{
			LinkLabel lb = ( LinkLabel )sender;
			Process.Start( lb.Text );
		}

		private void MailClicked( object sender, System.Windows.Forms.LinkLabelLinkClickedEventArgs e )
		{
			LinkLabel lb = ( LinkLabel )sender;
			Process.Start( "mailto:" + lb.Text );
		}

		private void Timer_Tick( object sender, System.EventArgs e )
		{
			if ( _diminish )
			{
				_picture.Width -= _step;
				if ( _picture.Width < _step )
				{
					_diminish = false;
					if ( _picture.Image == _logo2 )
					{
						_picture.Image = _logo1;
					}
					else
					{
						_picture.Image = _logo2;
					}
				}
			}
			else
			{
				_picture.Width += _step;
				if ( _picture.Width > _width-_step )
				{
					_diminish = true;

				}
			}
			_picture.Left = _left + ( _width-_picture.Width )/2;
		}
	}
}