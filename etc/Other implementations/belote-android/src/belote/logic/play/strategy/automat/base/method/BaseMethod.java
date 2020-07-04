/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.base.method;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.Team;
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceIterator;
import belote.bean.announce.AnnounceList;
import belote.bean.announce.AnnounceUnit;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.pack.Pack;
import belote.bean.pack.PackExtraAnnouncesManager;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.rank.Rank;
import belote.bean.pack.card.rank.RankIterator;
import belote.bean.pack.card.suit.Suit;
import belote.bean.pack.card.suit.SuitList;
import belote.bean.pack.sequence.Sequence;
import belote.bean.pack.sequence.SequenceIterator;
import belote.bean.pack.sequence.SequenceList;
import belote.bean.pack.sequence.SequenceType;
import belote.bean.trick.Trick;
import belote.bean.trick.TrickListIterator;
import belote.logic.play.strategy.automat.base.PlayCardMethod;

/**
 * BaseMethod class. Based class of all AI methods. Contains several helper methods used in the concrete methods.
 * @author Dimitar Karamanov
 */
public abstract class BaseMethod implements PlayCardMethod {

    /**
     * Single card count constant.
     */
    protected static final int SINGLE_CARD_COUNT = 1;

    /**
     * Two cards count constant.
     */
    protected static final int TWO_CARDS_COUNT = 2;
    
    /**
     * Three cards count constant.
     */
    protected static final int THREE_CARDS_COUNT = 3;
    
    /**
     * Four cards count constant.
     */
    protected static final int FOUR_CARDS_COUNT = 4;

    /**
     * Minimum suit's card number used to determine dominant suit.
     */
    protected static final int MINIMUM_SUIT_COUNT = 3;

    /**
     * Fit suit count.
     */
    protected static final int FIT_SUIT_COUNT = 4;

    /**
     * First defense position
     */
    private static final int FIRST_DEFENCE_POSITION = 1;

    /**
     * Second defense position
     */
    private static final int SECOND_DEFENCE_POSITION = 2;

    /**
     * Third defense position
     */
    private static final int THIRD_DEFENCE_POSITION = 3;

    /**
     * Belote game internal object.
     */
    protected final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public BaseMethod(final Game game) {
        this.game = game;
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public final Card getPlayerCard(final Player player) {
        // BelotLogicTest.addLog("Method --> " + getClass().getName());

        final Card result = getPlayMethodCard(player);
        if (result != null) {
            result.setCardAcquireMethod(getClass().getSimpleName());
        }
        return result;
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    protected abstract Card getPlayMethodCard(final Player player);

    /**
     * Returns true if only the team's players has cards from the provided suit, false otherwise.
     * 
     * @param suit provided suit.
     * @param team provided team.
     * @return boolean true if only the team's players has cards with the provided suit, false otherwise.
     */
    protected final boolean isTeamSuit(final Suit suit, final Team team) {
        boolean result = false;

        final Team enemyTeam = game.getOppositeTeam(team);
        for (final TrickListIterator iterator = game.getTrickList().iterator(); iterator.hasNext() && !result;) {
            final Trick trick = iterator.next();
            final Player attackPlayer = trick.getAttackPlayer();
            final Card attackCard = trick.getPlayerCard(attackPlayer);

            if (attackPlayer.getTeam().equals(team) && attackCard.getSuit().equals(suit)) {
                result = true;
                for (int i = 0; i < enemyTeam.getPlayersCount(); i++) {
                    final Card card = trick.getPlayerCard(enemyTeam.getPlayer(i));
                    if (card.getSuit().equals(suit)) {
                        result = false;
                        break;
                    }
                }
            }
        }
        return result;
    }
    
    protected final boolean isPartnerPossibleQuintOrQuarteSuit(final Suit suit, final Player player) {
        Player partner = player.getPartner();
        SequenceList sequences = partner.getCards().getSequencesList();
        for (SequenceIterator iterator = sequences.iterator(); iterator.hasNext();) {
            Sequence sequence = iterator.next();
            if (SequenceType.Quint.equals(sequence.getType()) && isPartnerPossibleSequenceSuit(SequenceType.Quint, suit, player)) {
                return true;
            }
            
            if (SequenceType.Quarte.equals(sequence.getType()) && isPartnerPossibleSequenceSuit(SequenceType.Quarte, suit, player)) {
                return true;
            }
        }
        
        return false;
    }
    
    protected final boolean isPartnerPossibleSequenceSuit(final SequenceType type, final Suit suit, final Player player) {
        int sequenceCount = getSequenceCount(type);
        int found = 0;
        
        if (player.getPartner().getCards().getSize() >= sequenceCount) {
            for (RankIterator iterator = Rank.iterator(); iterator.hasNext();) {
                Rank rank = iterator.next();

                if (player.getCards().findCard(rank, suit) != null) {
                    found = 0;
                }

                if (isPassedCard(rank, suit, true)) {
                    found = 0;
                }

                found++;

                if (found == sequenceCount) {
                    return true;
                }
            }
        }
        
        return false;
    }
    
    private int getSequenceCount(final SequenceType type) {
        if (SequenceType.Quint.equals(type)) {
            return PackExtraAnnouncesManager.ST_100_COUNT;
        }
        
        if (SequenceType.Quarte.equals(type)) {
            return PackExtraAnnouncesManager.ST_050_COUNT;
        }
        
        return PackExtraAnnouncesManager.ST_020_COUNT;
    }

    /**
     * Returns true if one or more team players has declared announce with the provided suit.
     * @param team provided team.
     * @param suit provided suit.
     * @return boolean true if the team players has announce with the provided suit, false otherwise.
     */
    protected boolean isTeamSuitAnnounce(final Team team, final Suit suit) {
        for (int i = 0; i < team.getPlayersCount(); i++) {
            final Player player = team.getPlayer(i);
            if (isPlayerSuitAnnounce(player, suit)) {
                return true;
            }
        }
        return false;
    }
    
    /**
     * Returns true if one or more team players has declared announce with the provided suit.
     * @param team provided team.
     * @param suit provided suit.
     * @return boolean true if the team players has announce with the provided suit, false otherwise.
     */
    protected boolean isPlayerSuitAnnounce(final Player player, final Suit suit) {
        final AnnounceSuit announceSuit = AnnounceUnit.transformFromSuitToAnnounceSuit(suit);
        final AnnounceList playerAnnounces = game.getAnnounceList().getPlayerAnnounces(player);

        for (final AnnounceIterator iterator = playerAnnounces.iterator(); iterator.hasNext();) {
            final Announce announce = iterator.next();
            if (announce.getAnnounceSuit().equals(announceSuit)) {
                return true;
            }
        }
        return false;
    }


    /**
     * Returns true if the suit is preferred from one ore more team players, false otherwise.
     * @param team which is checked
     * @param suit which is checked for team preferred.
     * @return boolean true if the suit is prefer from one ore more team players, false otherwise.
     */
    protected boolean isTeamPreferredSuit(final Team team, final Suit suit) {
        for (int i = 0; i < team.getPlayersCount(); i++) {
            final Player player = team.getPlayer(i);
            if (player.getPreferredSuits().contains(suit)) {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns true if the provided card is the maximum "meter" suit card left, false otherwise.
     * @param card provided card.
     * @param checkHand if true will check and hand's cards.
     * @return boolean true if the provided card is the maximum "meter" suit card left, false otherwise.
     */
    protected final boolean isMaxSuitCardLeft(final Card card, final boolean checkHand) {
        for (final RankIterator iterator = Rank.iterator(); iterator.hasNext();) {
            final Rank rank = iterator.next();
            if (card.compareRankTo(rank) < 0) {
                if (!isPassedCard(rank, card.getSuit(), checkHand)) {
                    return false;
                }
            }
        }
        return true;
    }

    /**
     * Returns true if the provided card is passed, false otherwise.
     * @param rank card's rank.
     * @param suit card's suit.
     * @param currentRoundCards flag determines if the current round cards are considered as passed or not.
     * @return boolean true if the provided card is passed, false otherwise.
     */
    protected final boolean isPassedCard(final Rank rank, final Suit suit, final boolean currentRoundCards) {
        // Check in teams hands cards
        for (int i = 0; i < game.getTeamsCount(); i++) {
            if (game.getTeam(i).getHands().findCard(rank, suit) != null) {
                return true;
            }
        }

        // Check in the current round cards
        if (currentRoundCards && game.getTrickCards().findCard(rank, suit) != null) {
            return true;
        }

        return false;
    }

    /**
     * Returns true if the card is "meter" suit card, false otherwise.
     * @param player provided player.
     * @param card check card.
     * @return boolean true if the card is "meter" suit card, false otherwise.
     */
    protected final boolean isMeterSuitCard(final Player player, final Card card) {
        final int passedSuitCardsCount = getPassedSuitCardsCount(card.getSuit());
        final int playerSuitCardsCount = player.getCards().getSuitCount(card.getSuit());

        return playerSuitCardsCount + passedSuitCardsCount == Rank.getRankCount();
    }

    /**
     * Returns the number of passed suit cards.
     * @param suit provided suit.
     * @return int number of passed suit cards.
     */
    protected final int getPassedSuitCardsCount(final Suit suit) {
        int result = 0;

        for (int i = 0; i < game.getTeamsCount(); i++) {
            result += game.getTeam(i).getHands().getSuitCount(suit);
        }
        result += game.getTrickCards().getSuitCount(suit);

        return result;
    }

    /**
     * Returns true if all cards of the provided player are hand cards(masters), false otherwise.
     * @param player provided one.
     * @return boolean true if all cards are hand cards(masters), false otherwise.
     */
    protected boolean isAllCardsMasters(final Player player) {
        for (final PackIterator iterator = player.getCards().iterator(); iterator.hasNext();) {
            final Card card = iterator.next();
            if (!isPlayerPowerSuit(player, card.getSuit())) {
                return false;
            }
        }
        return true;
    }

    /**
     * Returns true if only the provided player has card from the provided suit.
     * @param player provided player.
     * @param suit provided suit.
     * @return boolean true if the suit is power or false otherwise.
     */
    protected boolean isPlayerSuit(final Player player, final Suit suit) {
        return getPassedSuitCardsCount(suit) + player.getCards().getSuitCount(suit) == Rank.getRankCount();
    }

    /**
     * Returns true if the provided suit is power for the provided player or false otherwise.
     * @param player provided player.
     * @param suit provided suit.
     * @return boolean true if the suit is power or false otherwise.
     */
    protected boolean isPlayerPowerSuit(final Player player, final Suit suit) {
        final Pack playerCards = player.getCards().getSuitPack(suit);
        final Pack restCards = getOtherPlayersSuitCards(player, suit);

        do {
            final Card playerMaxSuitCard = playerCards.findMaxSuitCard(suit);
            final Card restMaxSuitCard = restCards.findMaxSuitCard(suit);
            final Card restMinSuitCard = restCards.findMinSuitCard(suit);

            if (playerMaxSuitCard == null) {
                return false;
            }
            if (restMaxSuitCard == null || restMinSuitCard == null) {
                return true;
            }
            if (playerMaxSuitCard.compareTo(restMaxSuitCard) < 0) {
                return false;
            }
            playerCards.remove(playerMaxSuitCard);
            restCards.remove(restMinSuitCard);
        } while (!playerCards.isEmpty() && !restCards.isEmpty());

        return true;
    }

    /**
     * Returns a pack holding the other 3 players(excluding the provided player) cards from a given suit. (excluding the passed and player' ones).
     * @param player provided player.
     * @param suit provided suit.
     * @return Pack with rest suit cards or empty.
     */
    protected final Pack getOtherPlayersSuitCards(final Player player, final Suit suit) {
        final Pack result = new Pack();
        // collect the rest suits cards from other players
        // it's honest because the player knows his and past cards
        for (int i = 0; i < game.getPlayersCount(); i++) {
            if (!game.getPlayer(i).equals(player)) {
                final Pack suitPack = game.getPlayer(i).getCards().getSuitPack(suit);
                result.addAll(suitPack);
            }
        }
        return result;
    }

    /**
     * Returns true if the provided player has a round attack, false otherwise.
     * @param player provided player.
     * @return boolean true if the provided player has a attack, false otherwise.
     */
    protected final boolean hasPlayerAttack(final Player player) {
        for (final TrickListIterator iterator = game.getTrickList().iterator(); iterator.hasNext();) {
            final Trick round = iterator.next();
            final Player attackPlayer = round.getAttackPlayer();
            if (attackPlayer.equals(player)) {
                return true;
            }
        }
        return false;
    }

    /**
     * Returns true if the enemy player' team declared the last normal announce, false otherwise.
     * @param player provided player.
     * @return boolean true if the enemy player' team declared the last normal announce, false otherwise.
     */
    protected final boolean isEnemyTeamAnnounce(final Player player) {
        final Announce lastNotPassAnnounce = game.getAnnounceList().getOpenContractAnnounce();
        return lastNotPassAnnounce != null && !lastNotPassAnnounce.getPlayer().isSameTeam(player);
    }

    /**
     * Returns true if the player' team declared the last normal announce, false otherwise.
     * @param player provided player.
     * @return boolean true if the player' team declared the last normal announce, false otherwise.
     */
    protected final boolean isPlayerTeamAnnounce(final Player player) {
        final Announce lastNotPassAnnounce = game.getAnnounceList().getOpenContractAnnounce();
        return lastNotPassAnnounce != null && lastNotPassAnnounce.getPlayer().isSameTeam(player);
    }

    /**
     * Returns true if the current game position is first defense, false otherwise.
     * @return true if the current game position is first defense, false otherwise.
     */
    protected final boolean isFirstDefencePosition() {
        return game.getTrickCards().getSize() == FIRST_DEFENCE_POSITION;
    }

    /**
     * Returns true if the current game position is second defense, false otherwise.
     * @return true if the current game position is second defense, false otherwise.
     */
    protected final boolean isSecondDefencePosition() {
        return game.getTrickCards().getSize() == SECOND_DEFENCE_POSITION;
    }

    /**
     * Returns true if the current game position is third defense, false otherwise.
     * @return true if the current game position is third defense, false otherwise.
     */
    protected final boolean isThirdDefencePosition() {
        return game.getTrickCards().getSize() == THIRD_DEFENCE_POSITION;
    }
    
    protected final Player getFirstDefencePlayer() {
        Player player = game.getTrickAttackPlayer();
        return game.getPlayerAfter(player);
    }
    
    protected final Player getSecondDefencePlayer() {
        Player player = getFirstDefencePlayer();
        return game.getPlayerAfter(player);
    }
    
    protected final Player getThirdDefencePlayer() {
        Player player = getSecondDefencePlayer();
        return game.getPlayerAfter(player);
    }
    
    protected final Suit getTrump() {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        if (announce != null) {
            return AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
        }
        return null;
    }
    
    protected boolean hasTrickAttackSuit(final Suit suit) {
        for (final TrickListIterator iterator = game.getTrickList().iterator(); iterator.hasNext();) {
            final Trick round = iterator.next();
            Card card = round.getTrickCards().getFirstNoNullCard();
            if (card != null && card.getSuit().equals(suit)) {
                return true;            
            }
        }
        return false;
    }
    
    protected SuitList getTrumpAnnounces(final Player player) {
        SuitList suits = new SuitList();
    
        final AnnounceList announces = game.getAnnounceList().getPlayerAnnounces(player);

        for (final AnnounceIterator iterator = announces.iterator(); iterator.hasNext();) {
            final Announce announce = iterator.next();
            if (announce.isTrumpAnnounce()) {
                final Suit suit = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
                suits.add(suit);
            }
        }
        
        return suits;
    }
}
