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

import belote.bean.announce.AnnounceList;
import belote.bean.pack.Pack;
import belote.bean.pack.PackIterator;
import belote.bean.pack.TrickPack;
import belote.bean.pack.card.Card;
import belote.bean.trick.TrickList;

/**
 * Game class.
 * @author Dimitar Karamanov
 */
public final class Game implements Serializable {

    /**
     * serialVersionUID
     */
    private static final long serialVersionUID = -6670321224692156622L;

    /**
     * Players count constant.
     */
    private final static int PLAYERS_COUNT = 4;

    /**
     * Teams count constant.
     */
    private final static int TEAMS_COUNT = 2;

    /**
     * Hanged points.
     */
    private int hangedPoints = 0;

    /**
     * Announce card number constant.
     */
    public final static int BELOT_ANNOUNCE_CARD_COUNT = 5;

    /**
     * End game points constant.
     */
    public final static int END_GAME_POINTS = 151;

    /**
     * Tricks list.
     */
    private final TrickList trickList = new TrickList();

    /**
     * Announce list.
     */
    private final AnnounceList announceList = new AnnounceList();

    /**
     * Game players.
     */
    private final Player[] players = new Player[PLAYERS_COUNT];

    /**
     * Game teams.
     */
    private final Team[] teams = new Team[TEAMS_COUNT];

    /**
     * Game pack.
     */
    private Pack pack;

    /**
     * Hand cards.
     */
    private final TrickPack trickCards = new TrickPack();

    /**
     * Game mode.
     */
    private GameMode gameMode = GameMode.AnnounceGameMode;

    /**
     * Announce attack player.
     */
    private Player dealAttackPlayer;

    /**
     * Trick attack player.
     */
    private Player trickAttackPlayer;

    /**
     * Couple player.
     */
    private Player trickCouplePlayer;

    /**
     * Constructor.
     * @param names of the players.
     */
    public Game() {
        for (int i = 0; i < teams.length; i++) {
            teams[i] = new Team(i);
        }

        for (int i = 0; i < players.length; i++) {
            players[i] = new Player(teams[i % teams.length], i);
        }

        setDealAttackPlayer(getPlayer(0));
        setTrickAttackPlayer(getPlayer(0));
    }

    /**
     * Initializes new full cards pack.
     */
    public void initPack() {
        pack = Pack.createFullPack();
        pack.shuffle();
        pack.nullCardAcquireMethod();
    }

    /**
     * Returns the size of card pack.
     * @return pack size.
     */
    public int getPackSize() {
        if (pack == null) {
            return 0;
        }
        return pack.getSize();
    }

    /**
     * Returns the attack player for current dealing.
     * @return Player deal attack player.
     */
    public Player getDealAttackPlayer() {
        return dealAttackPlayer;
    }

    /**
     * Sets the new deal attack player.
     * @param dealAttackPlayer new one.
     */
    public void setDealAttackPlayer(final Player dealAttackPlayer) {
        this.dealAttackPlayer = dealAttackPlayer;
    }

    /**
     * Returns the attack player for the current trick.
     * @return Player trick attack player.
     */
    public Player getTrickAttackPlayer() {
        return trickAttackPlayer;
    }

    /**
     * Sets new trick attack player.
     * @param trickAttackPlayer new one.
     */
    public void setTrickAttackPlayer(final Player trickAttackPlayer) {
        this.trickAttackPlayer = trickAttackPlayer;
    }

    /**
     * Returns trick couple player.
     * @return Player trick couple player.
     */
    public Player getTrickCouplePlayer() {
        return trickCouplePlayer;
    }

    /**
     * Sets new trick couple player.
     * @param trickCouplePlayer new one.
     */
    public void setTrickCouplePlayer(final Player trickCouplePlayer) {
        this.trickCouplePlayer = trickCouplePlayer;
    }

    /**
     * Returns current game mode.
     * @return game mode;
     */
    public GameMode getGameMode() {
        return gameMode;
    }

    /**
     * Set a new game mode.
     * @param gameMode new value.
     */
    public void setGameMode(final GameMode gameMode) {
        this.gameMode = gameMode;
    }

    /**
     * Returns game's hanged points.
     * @return int game's hanged points.
     */
    public int getHangedPoints() {
        return hangedPoints;
    }

    /**
     * Sets hanged points.
     * @param value which will be added to hangedPoints.
     */
    public void increaseHangedPoints(final int value) {
        hangedPoints += value;
    }

    /**
     * Clears hanged points.
     */
    public void clearHangedPoints() {
        hangedPoints = 0;
    }

    /**
     * Adds count more cards to the players cards.
     * @param count the count of the added cards to each player.
     */
    public void addPlayersCards(final int count) {
        for (int i = 0; i < count; i++) {
            for (int j = 0; j < players.length; j++) {
                players[j].getCards().add(pack.remove(0));
            }
        }
    }

    /**
     * Returns next player (iterates in cycle).
     * @param player current player.
     * @return Player next player.
     */
    public Player getPlayerAfter(final Player player) {
        if (player.getID() < players.length - 1) {
            return players[player.getID() + 1];
        }
        return players[0];
    }

    /**
     * Returns previous player (iterates in cycle).
     * @param player current player.
     * @return Player previous player.
     */
    public Player getPlayerBefore(final Player player) {
        if (player.getID() > 0) {
            return players[player.getID() - 1];
        }
        return players[players.length - 1];
    }

    /**
     * Returns opposite team by team.
     * @param team provided team.
     * @return Team opposite team.
     */
    public Team getOppositeTeam(final Team team) {
        return team.equals(teams[0]) ? teams[1] : teams[0];
    }

    /**
     * Returns opposite team by player.
     * @param player provided player.
     * @return Team opposite team.
     */
    public Team getOppositeTeam(Player player) {
        return getOppositeTeam(player.getTeam());
    }

    /**
     * Returns Announce list.
     * @return announce list.
     */
    public AnnounceList getAnnounceList() {
        return announceList;
    }

    /**
     * Returns trick list.
     * @return TrickList instance.
     */
    public TrickList getTrickList() {
        return trickList;
    }

    /**
     * Returns cards of the current trick.
     * @return trick cards.
     */
    public TrickPack getTrickCards() {
        return trickCards;
    }

    /**
     * Returns the count of game players. (4 players)
     * @return players count.
     */
    public int getPlayersCount() {
        return players.length;
    }

    /**
     * Returns player by index.
     * @param index of the player.
     * @return player by index.
     */
    public Player getPlayer(final int index) {
        return players[index];
    }

    /**
     * Returns the count of game teams. (2 teams)
     * @return teams count.
     */
    public int getTeamsCount() {
        return teams.length;
    }

    /**
     * Returns team by index.
     * @param index of the player.
     * @return team by index.
     */
    public Team getTeam(final int index) {
        return teams[index];
    }

    /**
     * Returns player who played provided card.
     * @param card provided card.
     * @return Player player who played provided card.
     */
    public Player getPlayerByCard(final Card card) {
        Player result = getTrickAttackPlayer();
        for (PackIterator iterator = getTrickCards().iterator(); iterator.hasNext();) {
            final Card currentCard = iterator.next();
            if (currentCard.equals(card)) {
                return result;
            }

            result = getPlayerAfter(result);
        }
        return null;
    }

    /**
     * Returns true if is announce game mode false otherwise.
     * @return boolean true if is announce game mode false otherwise.
     */
    public boolean isAnnounceGameMode() {
        return getGameMode().equals(GameMode.AnnounceGameMode);
    }

    /**
     * Checks if there is a winner team.
     * @return Team winner team or null.
     */
    public Team getWinnerTeam() {
        Team result = null;
        boolean capotGame = false;

        for (int i = 0; i < getTeamsCount(); i++) {
            if (getTeam(i).getPointsInfo().getCapotPoints() > 0) {
                capotGame = true;
            }

            if (getTeam(i).getPoints().getAllPoints() >= END_GAME_POINTS
                    && getTeam(i).getPoints().getAllPoints() > getOppositeTeam(getTeam(i)).getPoints().getAllPoints()) {
                result = getTeam(i);
            }
        }

        return capotGame ? null : result;
    }
}
