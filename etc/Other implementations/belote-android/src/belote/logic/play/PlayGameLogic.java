/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play;

import belote.base.BelotException;
import belote.bean.Game;
import belote.bean.Player;
import belote.bean.Team;
import belote.bean.announce.Announce;
import belote.bean.announce.suit.AnnounceSuit;
import belote.bean.pack.card.Card;
import belote.logic.play.points.calculators.AllTrumpPointsCalculator;
import belote.logic.play.points.calculators.TrumpPointsCalculator;
import belote.logic.play.points.calculators.NotTrumpPointsCalculator;
import belote.logic.play.points.calculators.NullPointsCalculator;
import belote.logic.play.points.calculators.PointsCalculator;
import belote.logic.play.points.distributors.EqualGamePointsDistributor;
import belote.logic.play.points.distributors.LostGamePointsDistributor;
import belote.logic.play.points.distributors.NullPointsDistributor;
import belote.logic.play.points.distributors.PointsDistributor;
import belote.logic.play.points.distributors.WinGamePointsDistributor;
import belote.logic.play.strategy.AllTrumpPlayStrategy;
import belote.logic.play.strategy.BasePlayStrategy;
import belote.logic.play.strategy.TrumpPlayStrategy;
import belote.logic.play.strategy.NotTrumpPlayStrategy;
import belote.logic.play.strategy.NullPlayCardStrategy;

/**
 * PlayCardStrategyFacade class.
 * @author Dimitar Karamanov
 */
public final class PlayGameLogic {

    /**
     * Belote game internal object.
     */
    private final Game game;

    /**
     * Trump suit game strategy adviser.
     */
    private final BasePlayStrategy clPlayCardStrategy;

    /**
     * All trump game strategy adviser.
     */
    private final BasePlayStrategy atPlayCardStrategy;

    /**
     * Not trump game strategy adviser.
     */
    private final BasePlayStrategy ntPlayCardStrategy;

    /**
     * Null game strategy adviser.
     */
    private final BasePlayStrategy nullPlayCardStrategy;

    /**
     * Trump suit game points calculator.
     */
    private final PointsCalculator clPointsCalculator;

    /**
     * All trump game points calculator.
     */
    private final PointsCalculator atPointsCalculator;

    /**
     * Not trump game points calculator.
     */
    private final PointsCalculator ntPointsCalculator;

    /**
     * Null game points calculator.
     */
    private final PointsCalculator nullPointsCalculator;

    /**
     * Win game points distributor.
     */
    private final PointsDistributor winGamePointsDistributor;

    /**
     * Lost game points distributor.
     */
    private final PointsDistributor lostGamePointsDistributor;

    /**
     * Equal game points distributor.
     */
    private final PointsDistributor equalGamePointsDistributor;

    /**
     * Null points distributor.
     */
    private final PointsDistributor nullPointsDistributor;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public PlayGameLogic(final Game game) {
        this.game = game;
        // Play card strategies helpers
        clPlayCardStrategy = new TrumpPlayStrategy(game);
        atPlayCardStrategy = new AllTrumpPlayStrategy(game);
        ntPlayCardStrategy = new NotTrumpPlayStrategy(game);
        nullPlayCardStrategy = new NullPlayCardStrategy(game);
        // Game points calculators
        clPointsCalculator = new TrumpPointsCalculator(game);
        atPointsCalculator = new AllTrumpPointsCalculator(game);
        ntPointsCalculator = new NotTrumpPointsCalculator(game);
        nullPointsCalculator = new NullPointsCalculator(game);
        // Game points distributors
        winGamePointsDistributor = new WinGamePointsDistributor(game);
        lostGamePointsDistributor = new LostGamePointsDistributor(game);
        equalGamePointsDistributor = new EqualGamePointsDistributor(game);
        nullPointsDistributor = new NullPointsDistributor(game);
    }

    /**
     * Returns helper depending from the provided announce type.
     * @param announce provided announce.
     * @return BasePlayCardStrategy helper depending from the provided announce type.
     */
    private BasePlayStrategy getHelperByAnnounce(Announce announce) {
        if (announce == null) {
            return nullPlayCardStrategy;
        }
        if (AnnounceSuit.AllTrump.equals(announce.getAnnounceSuit())) {
            return atPlayCardStrategy;
        }
        if (AnnounceSuit.NotTrump.equals(announce.getAnnounceSuit())) {
            return ntPlayCardStrategy;
        }
        return clPlayCardStrategy;
    }

    /**
     * Returns points calculator depending from the provided announce type.
     * @param announce provided announce.
     * @return PointsCalculator depending from the provided announce type.
     */
    private PointsCalculator getPointsCalculatorByAnnounce(Announce announce) {
        if (announce == null) {
            return nullPointsCalculator;
        }
        if (AnnounceSuit.AllTrump.equals(announce.getAnnounceSuit())) {
            return atPointsCalculator;
        }
        if (AnnounceSuit.NotTrump.equals(announce.getAnnounceSuit())) {
            return ntPointsCalculator;
        }
        return clPointsCalculator;
    }

    /**
     * Returns PointsDistributor subclass instance depends of win/lost/equal game.
     * @return PointsDistributor subclass instance.
     */
    private PointsDistributor getPointsDistributor() {
        Announce normalAnnounce = game.getAnnounceList().getOpenContractAnnounce();

        if (normalAnnounce != null) {
            final Team announceTeam = normalAnnounce.getPlayer().getTeam();
            final Team oppositeTeam = game.getOppositeTeam(announceTeam);
            final int announceTeamPoints = announceTeam.getPointsInfo().getTotalPoints();
            final int oppositeTeamPoints = oppositeTeam.getPointsInfo().getTotalPoints();

            if (announceTeamPoints > oppositeTeamPoints) {
                return winGamePointsDistributor;
            }
            if (announceTeamPoints < oppositeTeamPoints) {
                return lostGamePointsDistributor;
            }
            return equalGamePointsDistributor;
        }
        return nullPointsDistributor;
    }

    /**
     * Returns playing card for the provided player (AI).
     * 
     * @param player provided player.
     * @return Card playing card for the provided player (AI).
     */
    public Card getPlayerCard(final Player player) throws BelotException {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        final Card card = getHelperByAnnounce(announce).getPlayerCard(player);

        if (card != null) {
            if (!getHelperByAnnounce(announce).validatePlayerCard(player, card)) {
                throw new BelotException("The play of card " + card.toString() + " is not valid");
            }
        }

        return card;
    }

    /**
     * Returns next attack player.
     * @return next attack player.
     */
    public Player getNextTrickAttackPlayer() {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        return getHelperByAnnounce(announce).getNextAttackPlayer();
    }

    /**
     * Calculates team points.
     */
    public void calculateTeamsPoints() {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        getPointsCalculatorByAnnounce(announce).calculateTeamsPoints();
    }

    /**
     * Distributes points to the teams.
     */
    public void distributeTeamsPoints() {
        getPointsDistributor().distributeTeamsPoints();
    }

    /**
     * Validates player card.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    public boolean validatePlayerCard(Player player, Card card) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        return getHelperByAnnounce(announce).validatePlayerCard(player, card);
    }

    /**
     * Returns if the provided player has couple.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if has a couple false otherwise.
     */
    public boolean hasPlayerCouple(Player player, Card card) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        return getHelperByAnnounce(announce).hasPlayerCouple(player, card);
    }
}