/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean.trick;

import java.io.Serializable;

import belote.bean.Player;
import belote.bean.pack.Pack;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;

/**
 * Trick class.
 * @author Dimitar Karamanov
 */
public final class Trick implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 1185817214486088258L;

    /**
     * Trick attack player.
     */
    private final Player attackPlayer;

    /**
     * Trick winner player.
     */
    private final Player winnerPlayer;

    /**
     * Trick couple player (It can be only one).
     */
    private final Player couplePlayer;

    /**
     * Trick cards.
     */
    private final Pack trickCards = new Pack();

    /**
     * Constructor.
     * @param attackPlayer trick attack player.
     * @param winnerPlayer trick winner player.
     * @param couplePlayer trick couple player.
     * @param trickCards trick cards.
     */
    public Trick(final Player attackPlayer, final Player winnerPlayer, final Player couplePlayer, final Pack trickCards) {
        this.attackPlayer = attackPlayer;
        this.winnerPlayer = winnerPlayer;
        this.couplePlayer = couplePlayer;

        this.trickCards.copyFrom(trickCards);
    }

    /**
     * Returns trick attack player.
     * @return Player attack player.
     */
    public Player getAttackPlayer() {
        return attackPlayer;
    }

    /**
     * Returns trick winner player (Which is the next trick attack player).
     * @return Player winner player.
     */
    public Player getWinnerPlayer() {
        return winnerPlayer;
    }

    /**
     * Returns the player who declared a couple during the current trick or null.
     * @return Player couple player.
     */
    public Player getCouplePlayer() {
        return couplePlayer;
    }

    /**
     * Returns trick cards. The count of cards is equal to player's count. (One card per player).
     * @return Pack trick cards.
     */
    public Pack getTrickCards() {
        return trickCards;
    }

    /**
     * Returns card played by provided player in the current trick.
     * @param player which card is looking for.
     * @return Card played by provided player.
     */
    public Card getPlayerCard(Player player) {
        int id = attackPlayer.getID();

        for (final PackIterator iterator = trickCards.iterator(); iterator.hasNext();) {
            final Card card = iterator.next();

            if (id == player.getID()) {
                return card;
            }

            if (++id == Player.PLAYERS_COUNT) {
                id = 0;
            }
        }

        return null;
    }

    /**
     * Returns cards(Pack) iterator for played cards.
     * @return PackIterator instance for played cards.
     */
    public PackIterator iterator() {
        return trickCards.iterator();
    }
}
