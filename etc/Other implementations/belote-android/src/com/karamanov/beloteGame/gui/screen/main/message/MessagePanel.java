/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.gui.screen.main.message;

import java.util.ArrayList;

import android.content.Context;
import android.graphics.Bitmap;
import android.graphics.Color;
import android.graphics.Typeface;
import android.view.Gravity;
import android.widget.ImageView;
import android.widget.TableLayout;
import android.widget.TableRow;
import android.widget.TextView;
import belote.bean.Player;

import com.karamanov.beloteGame.Belote;
import com.karamanov.beloteGame.R;
import com.karamanov.beloteGame.text.PlayerNameDecorator;

/**
 * MessagePanel class.
 * @author Dimitar Karamanov
 */
public class MessagePanel extends TableLayout {

    /**
     * Constructor.
     * @param parent component.
     */
    public MessagePanel(Context context, Player player, ArrayList<MessageData> messages) {
        super(context);

        TableRow row = new TableRow(context);
        TableRow.LayoutParams trp = new TableRow.LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.WRAP_CONTENT);
        trp.span = 2;
        TextView tv = new TextView(context);

        PlayerNameDecorator playerDecorator = new PlayerNameDecorator(player);

        tv.setText(playerDecorator.decorate(context));
        tv.setTextColor(Color.WHITE);
        tv.setGravity(Gravity.CENTER_HORIZONTAL);
        tv.setTypeface(Typeface.DEFAULT_BOLD);
        row.addView(tv);
        row.setBackgroundResource(R.drawable.message_title);
        tv.setLayoutParams(trp);
        addView(row);

        for (MessageData data : messages) {
            addMessage(data.getImage(), data.getMessage());
        }
    }

    /**
     * Clears messages.
     */
    public void clear() {
        removeAllViews();
    }

    /**
     * Adds a message to list.
     * @param image of message.
     * @param text of message.
     */
    private void addMessage(final Bitmap image, final String text) {
        int dip3 = Belote.fromPixelToDip(getContext(), 3);
        int dip5 = Belote.fromPixelToDip(getContext(), 5);
        TableRow row = new TableRow(getContext());

        if (image == null) {
            row.addView(new TextView(getContext()));
        } else {
            ImageView imageView = new ImageView(getContext());
            imageView.setImageBitmap(image);
            TableRow.LayoutParams trp = new TableRow.LayoutParams(LayoutParams.MATCH_PARENT, LayoutParams.WRAP_CONTENT);
            trp.rightMargin = dip5;
            imageView.setLayoutParams(trp);
            row.addView(imageView);
        }

        TextView message = new TextView(getContext());
        message.setText(text);
        message.setTextColor(Color.DKGRAY);
        message.setTypeface(Typeface.DEFAULT_BOLD);
        row.addView(message);

        row.setPadding(dip3, dip3, dip3, dip3);
        addView(row);
    }
}
