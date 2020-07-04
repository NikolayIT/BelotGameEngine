package com.karamanov.beloteGame.gui.screen.tricks;

import java.io.Serializable;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.view.Gravity;
import android.view.MotionEvent;
import android.widget.LinearLayout;
import android.widget.LinearLayout.LayoutParams;
import android.widget.ScrollView;
import belote.bean.Game;
import belote.bean.trick.Trick;
import belote.bean.trick.TrickList;
import belote.bean.trick.TrickListIterator;

import com.karamanov.beloteGame.R;

public final class TricksActivity extends Activity {

    public final static String BELOTE = "BELOTE";

    public TricksActivity() {
        super();
    }

    /** Called when the activity is first created. */
    @Override
    public void onCreate(Bundle savedInstanceState) {
        super.onCreate(savedInstanceState);

        Intent startingIntent = getIntent();
        if (startingIntent != null) {
            Bundle bundle = startingIntent.getExtras();
            if (bundle != null) {
                Serializable data = bundle.getSerializable(BELOTE);
                if (data instanceof Game) {
                    Game game = (Game) data;

                    ScrollView scroll = new ScrollView(this);

                    LinearLayout vertical = new LinearLayout(this);
                    vertical.setOrientation(LinearLayout.VERTICAL);

                    TrickList tricks = game.getTrickList();

                    for (TrickListIterator i = tricks.iterator(); i.hasNext();) {
                        Trick trick = i.next();
                        TrickView trickView = new TrickView(this, trick, game);
                        LinearLayout.LayoutParams params = new LinearLayout.LayoutParams(LayoutParams.WRAP_CONTENT, LayoutParams.WRAP_CONTENT);
                        params.gravity = Gravity.CENTER_HORIZONTAL;
                        trickView.setLayoutParams(params);
                        vertical.addView(trickView);
                    }

                    scroll.addView(vertical);
                    scroll.setBackgroundResource(R.drawable.score_bkg);
                    setContentView(scroll);
                }
            }
        }
    }

    @Override
    public boolean onTouchEvent(MotionEvent event) {
        if (event.getAction() == MotionEvent.ACTION_DOWN) {
            finish();
        }
        return super.onTouchEvent(event);
    }
}
