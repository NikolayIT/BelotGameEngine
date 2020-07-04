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

import java.io.Serializable;

import belote.bean.Player;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.announce.type.AnnounceType;
import belote.bean.pack.card.suit.Suit;

/**
 * Announce class.
 * @author Dimitar Karamanov
 */
public final class Announce implements Serializable {

    /**
	 * 
	 */
    private static final long serialVersionUID = -895817895417482869L;

    /**
     * Announce suit.
     */
    private final AnnounceSuit suit;

    /**
     * Announce type.
     */
    private final AnnounceType type;

    /**
     * Announce player.
     */
    private final Player player;

    /**
     * Constructor. Constructor is private so use the provided factory methods to create announce. On this way is impossible to create Pass double or redouble
     * announce.
     * @param suit announce suit.
     * @param type announce type.
     * @param player announce player.
     */
    private Announce(final AnnounceSuit suit, final AnnounceType type, final Player player) {
        this.suit = suit;
        this.type = type;
        this.player = player;
    }

    /**
     * Returns associated announce suit instance.
     * @return AnnounceSuit announce suit.
     */
    public AnnounceSuit getAnnounceSuit() {
        return suit;
    }

    /**
     * Returns associated announce type instance.
     * @return AnnounceType announce type.
     */
    public AnnounceType getType() {
        return type;
    }

    /**
     * Returns associated announce player instance.
     * @return Player announce player.
     */
    public Player getPlayer() {
        return player;
    }

    /**
     * Returns a string representation of the object. The return name is based on class short name. This method has to be used only for debug purpose when the
     * project is not compiled with obfuscating. Don't use this method to represent the object. When the project is compiled with obfuscating the class name is
     * not the same.
     * @return String a string representation of the object.
     */
    public String toString() {
        return suit.toString() + type.toString();
    }

    /**
     * Returns if announce suit is from color announce suit (Club, Diamond, Heart and Spade) or not.
     * @return boolean true if announce suit is color false otherwise.
     */
    public boolean isTrumpAnnounce() {
        return suit.isTrumpSuit();
    }

    /**
     * Returns AllTrump suit normal type announce for the provided player.
     * @param player announce player.
     * @return Announce all trump announce for the provided player.
     */
    public static Announce createATNormalAnnounce(final Player player) {
        return new Announce(AnnounceSuit.AllTrump, AnnounceType.Normal, player);
    }

    /**
     * Returns not trump normal announce for the provided player.
     * @param player announce player.
     * @return Announce not trump announce for the provided player.
     */
    public static Announce createNTNormalAnnounce(final Player player) {
        return new Announce(AnnounceSuit.NotTrump, AnnounceType.Normal, player);
    }

    /**
     * Returns a suit normal announce for the provided player.
     * @param player announce player.
     * @param suit of the announce.
     * @return Announce suit announce for the provided player.
     */
    public static Announce createSuitNormalAnnounce(final Player player, final Suit suit) {
        if (suit == null) {
            return null;
        }
        final AnnounceSuit announceSuit = AnnounceUnit.transformFromSuitToAnnounceSuit(suit);
        return new Announce(announceSuit, AnnounceType.Normal, player);
    }

    /**
     * Returns a suit normal announce for the provided player.
     * @param player announce player.
     * @param announceSuit of the announce.
     * @return Announce suit announce for the provided player.
     */
    public static Announce createSuitNormalAnnounce(final Player player, final AnnounceSuit announceSuit) {
        return new Announce(announceSuit, AnnounceType.Normal, player);
    }

    /**
     * Returns pass announce for the provided player.
     * @param player announce player.
     * @return Announce pass announce for the provided player.
     */
    public static Announce createPassAnnounce(final Player player) {
        return new Announce(AnnounceSuit.Pass, AnnounceType.Normal, player);
    }

    /**
     * Returns a double type announce based on the provided one and player. If the provided announce suit is Pass the result is the same announce. Otherwise is
     * created a new announce of the same suit, double type and for provided player.
     * @param announce from which is created the double one.
     * @param player announce player.
     * @return the same announce as provided if is Pass suit one or new of double type.
     */
    public static Announce createDoubleAnnounce(final Announce announce, final Player player) {
        if (AnnounceSuit.Pass.equals(announce.getAnnounceSuit())) {
            return announce;
        }
        return new Announce(announce.getAnnounceSuit(), AnnounceType.Double, player);
    }

    /**
     * Returns a redouble type announce based on the provided one and player. If the provided announce suit is Pass the result is the same announce. Otherwise
     * is created a new announce of the same suit, redouble type and for provided player.
     * @param announce from which is created the redouble one.
     * @param player announce player.
     * @return the same announce as provided one if is Pass suit one or new of redouble type.
     */
    public static Announce createRedoubleAnnounce(final Announce announce, final Player player) {
        if (AnnounceSuit.Pass.equals(announce.getAnnounceSuit())) {
            return announce;
        }
        return new Announce(announce.getAnnounceSuit(), AnnounceType.Redouble, player);
    }
}
