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

import belote.base.Assert;
import belote.base.ComparableObject;
import belote.bean.pack.Pack;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceList;
import belote.bean.pack.square.Square;
import belote.bean.pack.square.SquareIterator;
import belote.bean.pack.square.SquareList;
import belote.bean.points.PointsInfo;
import belote.bean.points.PointsList;

/**
 * Team class.
 * @author Dimitar Karamanov
 */
public final class Team implements Serializable {

    /**
	 * SerialVersionUID
	 */
    private static final long serialVersionUID = 3269463892642834632L;

    /**
     * Capot extra points.
     */
    private static final int CAPOT_EXTRA_POINTS = 90;

    /**
     * Team players count.
     */
    private final static int TEAM_PLAYERS_COUNT = 2;

    /**
     * Team's player array.
     */
    private final Player[] players = new Player[TEAM_PLAYERS_COUNT];

    /**
     * Team's hands.
     */
    private final Pack hands = new Pack();

    /**
     * Team's couples.
     */
    private final Couple couples = new Couple();

    /**
     * Team's points list.
     */
    private final PointsList points = new PointsList();

    /**
     * Big games points.
     */
    private int winBelotGames = 0;

    /**
     * Team's ID.
     */
    private final int ID;

    /**
     * Game points info class.
     */
    private final PointsInfo pointsInfo = new PointsInfo();

    /**
     * Constructor.
     * 
     * @param ID of the team.
     */
    public Team(final int ID) {
        this.ID = ID;
    }

    /**
     * Sets team player.
     * @param player provided player.
     * @param index player's index.
     */
    public void setPlayer(final Player player, final int index) {
        Assert.assertTrue(index >= 0 && index < players.length, "Player's index out of range");

        players[index] = player;
    }

    /**
     * Returns PointsInfo.
     * @return PointsInfo.
     */
    public PointsInfo getPointsInfo() {
        return pointsInfo;
    }

    /**
     * Returns teams big games info.
     * @return int bigGames.
     */
    public int getWinBelotGames() {
        return winBelotGames;
    }

    /**
     * Increases winBelotGames.
     */
    public void increaseWinBelotGames() {
        winBelotGames++;
    }

    /**
     * Returns provided player partner.
     * @param player provided player.
     * @return Player partner.
     */
    public Player getPartner(final Player player) {
        if (player.equals(players[0])) {
            return players[1];
        }
        if (player.equals(players[1])) {
            return players[0];
        }

        throw new RuntimeException("The player is not team player");
    }

    /**
     * Returns hands points.
     * @return int hands points.
     */
    public int getHandsPoints() {
        int result = 0;
        for (PackIterator iterator = getHands().iterator(); iterator.hasNext();) {
            result += iterator.next().getPoints();
        }
        return result;
    }

    /**
     * Returns announce points.
     * @return int announce points.
     */
    public int getAnnouncePoints() {
        int result = 0;

        for (int i = 0; i < players.length; i++) {
            result += players[i].getCards().getAnnouncePoints();
        }

        return result;
    }

    /**
     * Returns capot points.
     * @return int capot points.
     */
    public int getCapotPoints() {
        if (getHands().getSize() == Rank.getRankCount() * Suit.getSuitCount()) {
            return CAPOT_EXTRA_POINTS;
        }
        return 0;
    }

    /**
     * Returns the maximum square of the team players.
     * @return EqualCards the maximum square.
     */
    public Square getMaxSquare() {
        Square result = null;

        for (int i = 0; i < players.length; i++) {
            final SquareList squaresList = players[i].getCards().getSquaresList();
            for (final SquareIterator iterator = squaresList.iterator(); iterator.hasNext();) {
                final Square square = iterator.next();
                if (result == null || square.compareTo(result) > 0) {
                    result = square;
                }
            }
        }
        return result;
    }

    /**
     * Returns maximum sequence of the team players.
     * @return Sequence maximum sequence.
     */
    public Sequence getMaxSequence() {
        Sequence result = null;

        for (int i = 0; i < players.length; i++) {
            final SequenceList sequencesList = players[i].getCards().getSequencesList();
            for (final SequenceIterator iterator = sequencesList.iterator(); iterator.hasNext();) {
                Sequence sequence = iterator.next();
                if (result == null || sequence.compareTo(result) > 0) {
                    result = sequence;
                }
            }
        }
        return result;
    }

    /**
     * Compares announces with announces of the provided team.
     * @param team provided team.
     * @return int value which may be: = 0 if this team announces are equal to the provided team announces > 0 if this team announces are bigger to the provided
     *         team announces < 0 if this team announces are smaller to the provided team announces
     */
    public int compareAnnouncesTo(final Team team) {
        int result = compareSquaresTo(team);

        if (result == 0) {
            return compareSequencesTo(team);
        } else {
            return result;
        }
    }

    /**
     * Compares equals with equals of the provided team.
     * @param team provided team.
     * @return int value which may be: = 0 if this team equals are equal to the provided team equals > 0 if this team equals are bigger to the provided team
     *         equals < 0 if this team equals are smaller to the provided team equals
     */
    private int compareSquaresTo(final Team team) {
        final Square maxSquare = getMaxSquare();
        final Square teamMaxSquare = team.getMaxSquare();

        return ComparableObject.compare(maxSquare, teamMaxSquare);
    }

    /**
     * Compares sequence with sequence of the provided team.
     * @param team provided team.
     * @return int value which may be: = 0 if this team sequence are equal to the provided team sequence > 0 if this team sequence are bigger to the provided
     *         team sequence < 0 if this team sequence are smaller to the provided team sequence
     */
    private int compareSequencesTo(final Team team) {
        final Sequence maxSequence = getMaxSequence();
        final Sequence teamMaxSequence = team.getMaxSequence();

        return ComparableObject.compare(maxSequence, teamMaxSequence);
    }

    /**
     * Clears teams game data.
     */
    public void clearData() {
        hands.clear();
        couples.clear();
    }

    /**
     * Clears data used in Belote Game cycle.
     */
    public void clearBeloteGameData() {
        points.clear();
    }

    /**
     * Returns player's hash code.
     * @return int teams's hash code value.
     */
    public int hashCode() {
        int hash = 13;
        hash = 59 * hash + this.ID;
        return hash;
    }

    /**
     * The method checks if this team and specified object (team) are equal.
     * @param obj specified object.
     * @return boolean true if this team is equal to specified object and false otherwise.
     */
    public boolean equals(final Object obj) {
        if (obj instanceof Team) {
            return ID == ((Team) obj).ID;
        }
        return false;
    }

    /**
     * Returns points of played games in current rubber game.
     * @return points list.
     */
    public PointsList getPoints() {
        return points;
    }

    /**
     * Returns couples of team players.
     * @return the couples
     */
    public Couple getCouples() {
        return couples;
    }

    /**
     * Returns hands of the team (win trick cards).
     * @return the hands
     */
    public Pack getHands() {
        return hands;
    }

    /**
     * Returns team players count.
     * @return players count.
     */
    public int getPlayersCount() {
        return players.length;
    }

    /**
     * Returns player by index or throws Exception.
     * @param index of the player.
     * @return player by index.
     */
    public Player getPlayer(final int index) {
        return players[index];
    }
}
