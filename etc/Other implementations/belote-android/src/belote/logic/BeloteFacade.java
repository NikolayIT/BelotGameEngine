/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic;

import belote.base.BelotException;
import belote.bean.Game;
import belote.bean.GameMode;
import belote.bean.Player;
import belote.bean.Team;
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceUnit;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.pack.PackIterator;
import belote.bean.pack.TrickPack;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.suit.Suit;
import belote.bean.trick.Trick;
import belote.logic.announce.AnnounceGameLogic;
import belote.logic.play.PlayGameLogic;

/**
 * BeloteGame class. Facade and proxy class which is the base point for the logic game.
 * @author Dimitar Karamanov
 */
public class BeloteFacade {

    /**
     * Announce card number constant.
     */
    public final static int ANNOUNCE_CARD_COUNT = 5;

    /**
     * Announce factory.
     */
    private AnnounceGameLogic announceFactory;

    /**
     * Play card strategy facade.
     */
    private PlayGameLogic belotGameLogic;

    /**
     * Game bean object.
     */
    protected Game game;

    /**
     * Constructor the only one.
     * @param names of the players.
     */
    public BeloteFacade() {
        this(new Game());
    }

    /**
     * Constructor the only one.
     * @param names of the players.
     */
    public BeloteFacade(Game game) {
        setGame(game);
    }

    public final void setGame(Game game) {
        this.game = game;
        belotGameLogic = new PlayGameLogic(game);
        announceFactory = new AnnounceGameLogic(game);
    }

    public final Game getGame() {
        return game;
    }

    /**
     * Clears teams data.
     */
    private void clearTeamsData() {
        for (int i = 0; i < game.getTeamsCount(); i++) {
            game.getTeam(i).clearData();
        }
    }

    /**
     * Clears teams Belote game data.
     */
    private void clearTeamsBeloteGameData() {
        for (int i = 0; i < game.getTeamsCount(); i++) {
            game.getTeam(i).clearBeloteGameData();
        }
    }

    /**
     * Process players extra announces.
     */
    private void processExtraAnnounces() {
        for (int i = 0; i < game.getPlayersCount(); i++) {
            game.getPlayer(i).getCards().processExtraAnnounces();
        }
    }

    /**
     * Arranges players cards.
     */
    public void arrangePlayersCards() {
        Announce announce = game.getAnnounceList().getContractAnnounce();

        for (int i = 0; i < game.getPlayersCount(); i++) {
            arrangePlayerCards(game.getPlayer(i), announce);
        }
    }

    /**
     * Arranges player' cards.
     */
    private void arrangePlayerCards(final Player player, final Announce announce) {
        if (announce == null) {
            player.getCards().arrange();
        } else if (announce.getAnnounceSuit().equals(AnnounceSuit.AllTrump)) {
            player.getCards().arrangeAT();
        } else if (announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump)) {
            player.getCards().arrangeNT();
        } else {
            final Suit suit = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
            player.getCards().arrangeCL(suit);
        }
    }

    /**
     * Clears players data.
     */
    private void clearPlayersData() {
        for (int i = 0; i < game.getPlayersCount(); i++) {
            game.getPlayer(i).clearData();
        }
    }

    /**
     * Adds players cards.
     */
    private void dealAnnounceCards() {
        game.initPack();
        game.addPlayersCards(ANNOUNCE_CARD_COUNT);
    }

    /**
     * Adds 3 more cards to the players cards.
     */
    private void dealRestCards() {
        final int restCount = game.getPackSize() / game.getPlayersCount();
        game.addPlayersCards(restCount);
    }

    /**
     * Checks for new big game.
     */
    private void checkBeloteGameEnd() {
        final Team team = game.getWinnerTeam();
        if (team != null) {
            clearTeamsBeloteGameData();
            team.increaseWinBelotGames();
            game.clearHangedPoints();
        }
    }

    /**
     * Begin new game (initialization).
     */
    public final void newGame() {
        checkBeloteGameEnd();
        game.setGameMode(GameMode.AnnounceGameMode);
        game.getAnnounceList().clear();
        game.getTrickList().clear();
        clearTeamsData();
        clearPlayersData();
        dealAnnounceCards();
        arrangePlayersCards();
        processExtraAnnounces();
    }

    /**
     * Sets next deal attack player.
     */
    public final void setNextDealAttackPlayer() {
        final Player nextGameAttackPlayer = getPlayerAfter(game.getDealAttackPlayer());
        game.setDealAttackPlayer(nextGameAttackPlayer);
        game.setTrickAttackPlayer(nextGameAttackPlayer);
    }

    /**
     * Returns true if is announce game mode false otherwise.
     * @return boolean true if is announce game mode false otherwise.
     */
    public final boolean isAnnounceGameMode() {
        return game.getGameMode().equals(GameMode.AnnounceGameMode);
    }

    /**
     * Returns true if is playing game mode false otherwise.
     * @return boolean true if is playing game mode false otherwise.
     */
    public final boolean isPlayingGameMode() {
        return game.getGameMode().equals(GameMode.PlayGameMode);
    }

    /**
     * Returns next player (iterates in cycle).
     * @param player current player.
     * @return Player next player.
     */
    public final Player getPlayerAfter(final Player player) {
        return game.getPlayerAfter(player);
    }

    /**
     * Checks for announce end.
     * @return boolean true if is announce end false otherwise.
     */
    public final boolean canDeal() {
        if (game.getGameMode().equals(GameMode.AnnounceGameMode)) {
            return game.getAnnounceList().canDeal();
        }
        return false;
    }

    /**
     * Process next announce.
     */
    public final void processNextAnnounce() {
        announceFactory.processNextAnnounce();
    }

    /**
     * Returns next orderer player.
     * @return Player the next announce player which have to order.
     */
    public final Player getNextAnnouncePlayer() {
        return announceFactory.getAnnounceOrderPlayer();
    }

    /**
     * Deals and arranges rest cards.
     */
    public final void manageRestCards() {
        dealRestCards();
        processExtraAnnounces();
        arrangePlayersCards();
    }

    /**
     * Returns if the provided player has couple.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if has a couple false otherwise.
     */
    public final boolean hasPlayerCouple(Player player, Card card) {
        return belotGameLogic.hasPlayerCouple(player, card);
    }

    /**
     * Sets player's couple for the provided suit.
     * @param player provided player.
     * @param suit provided suit.
     */
    public final void setPlayerCouple(Player player, Suit suit) {
        player.getTeam().getCouples().setCouple(suit);
        game.setTrickCouplePlayer(player);
    }

    /**
     * Returns played card for the provided player.
     * @param player provided player.
     * @return Card played card for the provided player.
     */
    public final Card playSingleHand(Player player) throws BelotException {
        final Card card = belotGameLogic.getPlayerCard(player);
        if (card != null) {
            removePlayerCard(player, card);
        }
        return card;
    }
    
    private void removePlayerCard(Player player, Card card) {
        final Card attackCard = game.getTrickCards().getAttackCard();
        
        player.getCards().remove(card);
        game.getTrickCards().add(card);
        
        if (hasPlayerCouple(player, card)) {
            setPlayerCouple(player, card.getSuit());
        }
        
        if (attackCard == null && game.getTrickList().getAttackCount(player) < 3) {
            player.getPreferredSuits().add(card.getSuit());
        }
        
        if (attackCard != null && !attackCard.getSuit().equals(card.getSuit())) {
            player.getUnwantedSuits().add(card.getSuit());
            player.getMissedSuits().add(attackCard.getSuit());
        }
    }

    /**
     * Returns true if is end hand and clears hand.
     * @return boolean true if is end hand and clears hand.
     */
    public final boolean isTrickEnd() {
        return game.getTrickCards().getSize() == game.getPlayersCount();
    }

    /**
     * Returns null or next attack player.
     * @return null or Player instance.
     */
    public final Player getNextTrickAttackPlayer() {
        if (isTrickEnd()) {
            return belotGameLogic.getNextTrickAttackPlayer();
        }
        return null;
    }

    /**
     * Process trick data.
     */
    public final void processTrickData() {
        if (isTrickEnd()) {
            final Player attackPlayer = game.getTrickAttackPlayer();
            game.setTrickAttackPlayer(belotGameLogic.getNextTrickAttackPlayer());

            final Trick trick = new Trick(attackPlayer, game.getTrickAttackPlayer(), game.getTrickCouplePlayer(), game.getTrickCards());
            game.getTrickList().add(trick);

            if (game.getTrickAttackPlayer() != null) {
                game.getTrickAttackPlayer().getTeam().getHands().addAll(game.getTrickCards());
            }

            game.setTrickCouplePlayer(null);
            game.getTrickCards().clear();

            for (int i = 0; i < game.getPlayersCount(); i++) {
                game.getPlayer(i).setSelectedCard(null);
            }
        }
    }

    /**
     * Returns true if the game ended false otherwise.
     * @return boolean true if the game ended false otherwise.
     */
    public final boolean checkGameEnd() {
        for (int i = 0; i < game.getPlayersCount(); i++) {
            if (game.getPlayer(i).getCards().getSize() != 0) {
                return false;
            }
        }

        game.setGameMode(GameMode.InfoGameMode);
        return true;
    }

    /**
     * Calculates team's points.
     */
    public final void calculateTeamsPoints() {
        belotGameLogic.calculateTeamsPoints();
        belotGameLogic.distributeTeamsPoints();
    }

    /**
     * Validates player card.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    public final boolean validatePlayerCard(Player player, Card card) {
        if (player != null && card != null) {
            return belotGameLogic.validatePlayerCard(player, card);
        }
        return false;
    }

    /**
     * Process human played card. (Checks for couple, preferred, unwanted and missed suits information)
     * @param player human player.
     * @param card played card.
     */
    public final void processHumanPlayerCard(Player player, Card card) {
        removePlayerCard(player, card);

        Announce announce = game.getAnnounceList().getContractAnnounce();
        if (announce != null && announce.getAnnounceSuit().equals(AnnounceSuit.AllTrump) && Rank.Jack.equals(card.getRank())) {
            player.getJackAceSuits().add(card.getSuit());
        }

        if (announce != null && announce.getAnnounceSuit().equals(AnnounceSuit.NotTrump) && Rank.Ace.equals(card.getRank())) {
            player.getJackAceSuits().add(card.getSuit());
        }
    }

    /**
     * Set a new game mode.
     * @param gameMode new value.
     */
    public final void setGameMode(final GameMode gameMode) {
        game.setGameMode(gameMode);
    }

    /**
     * Returns player's trick card or null if hasn't played yet.
     * @param player which trick card is looking for.
     * @return Card instance or null.
     */
    public final Card getPlayerTrickCard(final Player player) {
        TrickPack trickCards = game.getTrickCards();
        final PackIterator iterator = trickCards.iterator();
        Card card = null;

        final Player startPlayer = game.getTrickAttackPlayer();
        Player currentPlayer = startPlayer;

        do {
            if (iterator.hasNext()) {
                card = iterator.next();
            } else {
                card = null;
            }

            if (currentPlayer.equals(player)) {
                return card;
            }

            currentPlayer = getPlayerAfter(currentPlayer);

        } while (!startPlayer.equals(currentPlayer));

        return null;
    }

    /**
     * Returns if the provided player is on trick turn or not.
     * @param player which trick turn is checked
     * @return boolean true or false.
     */
    public final boolean isPlayerTrickOrder(Player player) {
        TrickPack trickCards = game.getTrickCards();
        final PackIterator iterator = trickCards.iterator();
        Card card = null;

        final Player startPlayer = game.getTrickAttackPlayer();
        Player currentPlayer = game.getTrickAttackPlayer();

        do {
            if (iterator.hasNext()) {
                card = iterator.next();
            } else {
                card = null;
            }

            if (currentPlayer.equals(player)) {
                return card == null;
            } else if (card == null) {
                return false;
            }

            currentPlayer = getPlayerAfter(currentPlayer);

        } while (!startPlayer.equals(currentPlayer));

        return false;
    }
}
