package com.karamanov.beloteGame.gui.screen.main.announce;

import android.content.Context;
import android.graphics.Rect;
import android.widget.Button;

import com.karamanov.beloteGame.R;

public class AnnounceButtonField extends Button {

    public AnnounceButtonField(Context context, String caption) {
        super(context);

        setFocusable(true);
        setSoundEffectsEnabled(false);

        setText(caption);

        Rect bounds = new Rect();
        String notTrumps = context.getString(R.string.NotTrumpsAnnounce);
        getPaint().getTextBounds(notTrumps, 0, notTrumps.length(), bounds);
        setMinWidth(getPaddingLeft() + bounds.width() + getPaddingRight());
    }
}