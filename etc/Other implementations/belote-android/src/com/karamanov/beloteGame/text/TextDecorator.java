/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package com.karamanov.beloteGame.text;

import java.util.ArrayList;
import java.util.Hashtable;

import android.content.Context;
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceIterator;
import belote.bean.announce.AnnounceList;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.announce.type.AnnounceType;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.square.Square;

import com.karamanov.beloteGame.R;

/**
 * TextDecorator class.
 * @author Dimitar Karamanov
 */
public final class TextDecorator {

    /**
     * Announce types container.
     */
    private final Hashtable<AnnounceType, String> announceTypes = new Hashtable<AnnounceType, String>();

    /**
     * Announce suits container.
     */
    private final Hashtable<AnnounceSuit, String> announceSuits = new Hashtable<AnnounceSuit, String>();

    /**
     * Short announce types container.
     */
    private final Hashtable<AnnounceSuit, String> shortAnnounceSuits = new Hashtable<AnnounceSuit, String>();

    /**
     * Rank signs container.
     */
    private final Hashtable<Rank, String> rankSigns = new Hashtable<Rank, String>();

    /**
     * Rank signs container.
     */
    private final Hashtable<Rank, String> ranks = new Hashtable<Rank, String>();

    private final String doubleAnnounce;

    private final String redoubleAnnounce;

    private final Context context;

    /**
     * Constructor.
     */
    public TextDecorator(Context context) {
        this.context = context;

        // Announce types initialization
        announceTypes.put(AnnounceType.Normal, context.getString(R.string.OrdinaryAnnounce));
        announceTypes.put(AnnounceType.Double, context.getString(R.string.DoubleAnnounce));
        announceTypes.put(AnnounceType.Redouble, context.getString(R.string.RedoubleAnnounce));

        // Announce suits initialization
        announceSuits.put(AnnounceSuit.AllTrump, context.getString(R.string.AllTrumpsAnnounce));
        announceSuits.put(AnnounceSuit.Club, context.getString(R.string.ClubsAnnounce));
        announceSuits.put(AnnounceSuit.Diamond, context.getString(R.string.DiamondsAnnounce));
        announceSuits.put(AnnounceSuit.Heart, context.getString(R.string.HeartsAnnounce));
        announceSuits.put(AnnounceSuit.Spade, context.getString(R.string.SpadesAnnounce));
        announceSuits.put(AnnounceSuit.Pass, context.getString(R.string.PassAnnounce));
        announceSuits.put(AnnounceSuit.NotTrump, context.getString(R.string.NotTrumpsAnnounce));

        // Short announce suits initialization
        shortAnnounceSuits.put(AnnounceSuit.AllTrump, context.getString(R.string.AllTrumpsAnnounceShort));
        shortAnnounceSuits.put(AnnounceSuit.Club, context.getString(R.string.ClubsAnnounceShort));
        shortAnnounceSuits.put(AnnounceSuit.Diamond, context.getString(R.string.DiamondsAnnounceShort));
        shortAnnounceSuits.put(AnnounceSuit.Heart, context.getString(R.string.HeartsAnnounceShort));
        shortAnnounceSuits.put(AnnounceSuit.Spade, context.getString(R.string.SpadesAnnounceShort));
        shortAnnounceSuits.put(AnnounceSuit.Pass, context.getString(R.string.PassAnnounceShort));
        shortAnnounceSuits.put(AnnounceSuit.NotTrump, context.getString(R.string.NotTrumpsAnnounceShort));

        // Rank signs initialization
        rankSigns.put(Rank.Ace, context.getString(R.string.AceSign));
        rankSigns.put(Rank.King, context.getString(R.string.KingSign));
        rankSigns.put(Rank.Queen, context.getString(R.string.QueenSign));
        rankSigns.put(Rank.Jack, context.getString(R.string.JackSign));
        rankSigns.put(Rank.Ten, context.getString(R.string.TenSign));
        rankSigns.put(Rank.Nine, context.getString(R.string.NineSign));
        rankSigns.put(Rank.Eight, context.getString(R.string.EightSign));
        rankSigns.put(Rank.Seven, context.getString(R.string.SevenSign));

        // Rank signs initialization
        ranks.put(Rank.Ace, context.getString(R.string.Ace));
        ranks.put(Rank.King, context.getString(R.string.King));
        ranks.put(Rank.Queen, context.getString(R.string.Queen));
        ranks.put(Rank.Jack, context.getString(R.string.Jack));
        ranks.put(Rank.Ten, context.getString(R.string.Ten));
        ranks.put(Rank.Nine, context.getString(R.string.Nine));
        ranks.put(Rank.Eight, context.getString(R.string.Eight));
        ranks.put(Rank.Seven, context.getString(R.string.Seven));

        doubleAnnounce = context.getString(R.string.DoubleAnnounce);
        redoubleAnnounce = context.getString(R.string.RedoubleAnnounce);
    }

    /**
     * Returns announce type text.
     * @param announceType AnnounceType instance.
     * @return Text presentation of the provided argument object.
     */
    public String getAnnounceType(final AnnounceType announceType) {
        return getHasTableKeyString(announceTypes, announceType);
    }

    /**
     * Returns announce suit text.
     * @param announceSuit AnnounceSuit instance.
     * @return Text presentation of the provided argument object.
     */
    public String getAnnounceSuit(final AnnounceSuit announceSuit) {
        return getHasTableKeyString(announceSuits, announceSuit);
    }

    /**
     * Returns short announce suit text.
     * @param announceSuit AnnounceSuit instance.
     * @return Short text presentation of the provided argument object.
     */
    public String getAnnounceSuitShort(final AnnounceSuit announceSuit) {
        return getHasTableKeyString(shortAnnounceSuits, announceSuit);
    }

    /**
     * Returns rank sign text.
     * @param rank Rank instance.
     * @return Text presentation of the provided argument object.
     */
    public String getRankSign(final Rank rank) {
        return getHasTableKeyString(rankSigns, rank);
    }

    /**
     * Returns rank sign text.
     * @param rank Rank instance.
     * @return Text presentation of the provided argument object.
     */
    public String getRank(final Rank rank) {
        return getHasTableKeyString(ranks, rank);
    }

    /**
     * Returns rank first letter sign.
     * @param rank Rank instance.
     * @return Text presentation of the provided argument object.
     */
    public String getRankLetter(final Rank rank) {
        final String text = getHasTableKeyString(ranks, rank);
        if (text != null && text.length() > 0) {
            return text.substring(0, 1);
        }
        return "";
    }

    /**
     * Returns associated key object value text presentation.
     * @param hash container.
     * @param key of the object.
     * @return Text presentation of the key object.
     */
    private String getHasTableKeyString(final Hashtable<?, String> hash, final Object key) {
        if (hash.containsKey(key)) {
            return hash.get(key).toString();
        }
        return "";
    }

    /**
     * Returns text represention of the current game announce.
     * @param announceList
     * @return String game announce text.
     */
    public ArrayList<String> getAnnounceText(final AnnounceList announceList) {
        return getAnnounceText(announceList, true);
    }

    /**
     * Returns text represention of the current game announce.
     * @param announceList
     * @return String game announce text.
     */
    public ArrayList<String> getShortAnnounceText(final AnnounceList announceList) {
        return getAnnounceText(announceList, false);
    }

    /**
     * Returns text represention of the squere.
     * @param square instance.
     * @return String game announce text.
     */
    public String getSquareText(final Square square) {
        return String.valueOf(square.getPoints()) + "(4x" + getRankLetter(square.getRank()) + ")";
    }

    /**
     * Returns text represention of the current game announce.
     * @param normal indicates if is full or short version.
     * @return String game announce text.
     */
    private ArrayList<String> getAnnounceText(final AnnounceList announceList, final boolean normal) {
        ArrayList<String> result = new ArrayList<String>();
        // final StringBuffer result = new StringBuffer();
        final Announce lastNormalAnnounce = announceList.getOpenContractAnnounce();

        if (lastNormalAnnounce != null) {
            if (normal) {
                PlayerNameDecorator decorator = new PlayerNameDecorator(lastNormalAnnounce.getPlayer());
                result.add(getAnnounceSuitShort(lastNormalAnnounce.getAnnounceSuit()) + " " + decorator.decorate(context));
            } else {
                ShortPlayerNameDecorator decorator = new ShortPlayerNameDecorator(lastNormalAnnounce.getPlayer());
                result.add("(" + decorator.decorate(context) + ")");
            }

            AnnounceList announces = announceList.getSuitAnnounces(lastNormalAnnounce.getAnnounceSuit());

            for (final AnnounceIterator iterator = announces.iterator(); iterator.hasNext();) {
                Announce announce = iterator.next();

                if (announce.getType().equals(AnnounceType.Double)) {
                    if (normal) {
                        PlayerNameDecorator decorator = new PlayerNameDecorator(announce.getPlayer());
                        result.add(doubleAnnounce + " " + decorator.decorate(context));
                    } else {
                        ShortPlayerNameDecorator decorator = new ShortPlayerNameDecorator(announce.getPlayer());
                        result.add(doubleAnnounce + " " + decorator.decorate(context));
                    }
                }

                if (announce.getType().equals(AnnounceType.Redouble)) {
                    if (normal) {
                        PlayerNameDecorator decorator = new PlayerNameDecorator(announce.getPlayer());
                        result.add(redoubleAnnounce + " " + decorator.decorate(context));
                    } else {
                        ShortPlayerNameDecorator decorator = new ShortPlayerNameDecorator(announce.getPlayer());
                        result.add(redoubleAnnounce + " " + decorator.decorate(context));
                    }
                }
            }
        }
        return result;
    }

    public String getShortAnnounceTextEx(AnnounceList announceList) {
        return transformToString(getShortAnnounceText(announceList));
    }

    private String transformToString(ArrayList<String> v) {
        StringBuilder sb = new StringBuilder();
        for (String s : v) {
            if (sb.length() != 0) {
                sb.append(" ");
            }
            sb.append(s);
        }
        return sb.toString();
    }

    /**
     * Returns text representation of the current game announce.
     * @param announceList
     * @return String game announce text.
     */
    public String getAnnounceTextEx(final AnnounceList announceList) {
        return transformToString(getAnnounceText(announceList, true));
    }
}
