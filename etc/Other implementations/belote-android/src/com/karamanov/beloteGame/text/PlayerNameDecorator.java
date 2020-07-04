package com.karamanov.beloteGame.text;

import java.util.Hashtable;

import android.content.Context;
import belote.bean.Player;

import com.karamanov.beloteGame.R;

public class PlayerNameDecorator {

    private final static Hashtable<Integer, Integer> names = crateHashtable();

    private final Integer ID;

    public PlayerNameDecorator(Player player) {
        ID = Integer.valueOf(player.getID());
    }

    private static Hashtable<Integer, Integer> crateHashtable() {
        Hashtable<Integer, Integer> result = new Hashtable<Integer, Integer>();

        result.put(0, R.string.North);
        result.put(1, R.string.West);
        result.put(2, R.string.South);
        result.put(3, R.string.East);

        return result;
    }

    public String decorate(Context context) {
        Integer integer = names.get(ID);
        if (integer == null) {
            return ID.toString();
        }
        return context.getString(integer);
    }
}
