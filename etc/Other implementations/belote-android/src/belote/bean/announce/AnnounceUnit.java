/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.announce;

import java.util.Enumeration;
import java.util.Hashtable;

import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.pack.card.suit.Suit;

/**
 * Transforms from Suit to AnnounceSuit and back.
 * @author Dimitar Karamanov
 */
public class AnnounceUnit {

    /**
     * Mapping which maps AnnounceSuit to Suit. (Only suit announces have corresponding suit).
     */
    private final static Hashtable<Suit, AnnounceSuit> mapping = initMapping();

    /**
     * Initializes mapping table.
     * @return Hashtable with suit mappings.
     */
    private static Hashtable<Suit, AnnounceSuit> initMapping() {
        final Hashtable<Suit, AnnounceSuit> result = new Hashtable<Suit, AnnounceSuit>();

        result.put(Suit.Club, AnnounceSuit.Club);
        result.put(Suit.Diamond, AnnounceSuit.Diamond);
        result.put(Suit.Heart, AnnounceSuit.Heart);
        result.put(Suit.Spade, AnnounceSuit.Spade);

        return result;
    }

    /**
     * Transforms from Suit object to AnnounceSuit one.
     * @param suit provided suit.
     * @return AnnounceSuit instance object.
     */
    public static final AnnounceSuit transformFromSuitToAnnounceSuit(final Suit suit) {
        if (mapping.containsKey(suit)) {
            return mapping.get(suit);
        }
        return AnnounceSuit.Pass;
    }

    /**
     * Transforms from AnnounceSuit object to Suit one.
     * @param announceSuit provided announce suit.
     * @return Suit instance object or null.
     */
    public final static Suit transformFromAnnounceSuitToSuit(final AnnounceSuit announceSuit) {
        for (final Enumeration<Suit> iterator = mapping.keys(); iterator.hasMoreElements();) {
            final Suit suit = iterator.nextElement();
            final AnnounceSuit aSuit = mapping.get(suit);
            if (aSuit.equals(announceSuit)) {
                return suit;
            }
        }
        return null;
    }
}
