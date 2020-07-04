/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.bean;

import java.io.Serializable;

import belote.bean.pack.Pack;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.SuitList;

/**
 * Player class.
 * @author Dimitar Karamanov
 */
public final class Player implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 7234505479951609925L;

    /**
     * Player's name.
     */
    private final String name;

    /**
     * Player's cards.
     */
    private final Pack cards = new Pack();

    /**
     * Player's team.
     */
    private final Team team;

    /**
     * Player's ID.
     */
    private final int ID;

    /**
     * Player's preferred suits.
     */
    private final SuitList preferredSuits = new SuitList();

    /**
     * Player's unwanted suits.
     */
    private final SuitList unwantedSuits = new SuitList();

    /**
     * Player's missed suits.
     */
    private final SuitList missedSuits = new SuitList();

    /**
     * AT|NT marked by Ace or Jack suits.
     */
    private final SuitList jackAceSuits = new SuitList();

    /**
     * Game's players count constant.
     */
    public static final int PLAYERS_COUNT = 4;

    /**
     * Player selected card.
     */
    private Card selectedCard = null;

    /**
     * Constructor.
     * @param name of the player.
     * @param team of the player.
     * @param ID of the player.
     */
    public Player(final Team team, final int ID) {
        this.name = "";
        this.team = team;
        team.setPlayer(this, ID / 2);
        this.ID = ID;
    }

    /**
     * Returns player's name.
     * @return String player's name.
     */
    public String getName() {
        return name;
    }

    /**
     * Returns player's partner.
     * @return Player partner.
     */
    public Player getPartner() {
        return team.getPartner(this);
    }

    /**
     * Returns player's team.
     * @return Team player's team .
     */
    public Team getTeam() {
        return team;
    }

    /**
     * Returns true if the players are team-mates, false otherwise.
     * @param player
     * @return boolean true or false.
     */
    public boolean isSameTeam(final Player player) {
        return team.equals(player.team);
    }

    /**
     * Clears players game data.
     */
    public void clearData() {
        cards.clear();
        jackAceSuits.clear();
        preferredSuits.clear();
        unwantedSuits.clear();
        missedSuits.clear();
    }

    /**
     * Returns player's hash code.
     * @return int player's hash code value.
     */
    public int hashCode() {
        int hash = 7;
        hash = 59 * hash + ID;
        return hash;
    }

    /**
     * The method checks if this player and specified object (player) are equal.
     * @param obj specified object
     * @return boolean true if this player is equal to specified object and false otherwise
     */
    public boolean equals(final Object obj) {
        if (obj instanceof Player) {
            return ID == ((Player) obj).ID;
        }
        return false;
    }

    /**
     * @return the cards
     */
    public Pack getCards() {
        return cards;
    }

    /**
     * @return the ID
     */
    public int getID() {
        return ID;
    }

    /**
     * @return the preferredSuits
     */
    public SuitList getPreferredSuits() {
        return preferredSuits;
    }

    public SuitList getJackAceSuits() {
        return jackAceSuits;
    }

    /**
     * @return the unwantedSuits
     */
    public SuitList getUnwantedSuits() {
        return unwantedSuits;
    }

    /**
     * @return the missedSuits
     */
    public SuitList getMissedSuits() {
        return missedSuits;
    }

    /**
     * @return the humanSelectedCard
     */
    public Card getSelectedCard() {
        return selectedCard;
    }

    /**
     * Sets player selected card.
     * @param selectedCard the player selected one.
     */
    public void setSelectedCard(Card selectedCard) {
        this.selectedCard = selectedCard;
    }
}
