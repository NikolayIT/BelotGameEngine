/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy;

import belote.base.Assert;
import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.base.PlayCardMethod;
import belote.logic.play.strategy.validators.Validatable;

/**
 * BasePlayCardStrategy class. Base strategy playing class.
 * @author Dimitar Karamanov
 */
public abstract class BasePlayStrategy {

    /**
     * Attack card method executor.
     */
    private final PlayCardMethod attackCardMethodExecutor;

    /**
     * First defense position card method executor.
     */
    private final PlayCardMethod firstDefencePositionCardMethodExecutor;

    /**
     * Second defense position card method executor.
     */
    private final PlayCardMethod secondDefencePositionCardMethodExecutor;

    /**
     * Third defense position card method executor.
     */
    private final PlayCardMethod thirdDefencePositionCardMethodExecutor;

    /**
     * Belote game internal object.
     */
    protected final Game game;

    /**
     * Validator for the player' move.
     */
    protected final Validatable validator;

    /**
     * Constructor.
     * @param game belote game instance.
     * @param validator a game validator instance.
     * @param playCardMethod
     */
    public BasePlayStrategy(final Game game, final Validatable validator, final PlayCardMethod playCardMethod) {
        this(game, validator, playCardMethod, playCardMethod, playCardMethod, playCardMethod);
    }

    /**
     * Constructor.
     * @param game belote game instance.
     * @param validator a game validator instance.
     * @param attackCardMethodExecutor
     * @param defencePositionCardMethodExecutor
     */
    public BasePlayStrategy(final Game game, final Validatable validator, final PlayCardMethod attackCardMethodExecutor,
            final PlayCardMethod defencePositionCardMethodExecutor) {
        this(game, validator, attackCardMethodExecutor, defencePositionCardMethodExecutor, defencePositionCardMethodExecutor, defencePositionCardMethodExecutor);
    }

    /**
     * Constructor.
     * @param game belote game instance.
     * @param validator a game validator instance.
     * @param attackCardMethodExecutor
     * @param firstDefencePositionCardMethodExecutor
     * @param secondDefencePositionCardMethodExecutor
     * @param thirdDefencePositionCardMethodExecutor
     */
    public BasePlayStrategy(final Game game, final Validatable validator, final PlayCardMethod attackCardMethodExecutor,
            final PlayCardMethod firstDefencePositionCardMethodExecutor, final PlayCardMethod secondDefencePositionCardMethodExecutor,
            final PlayCardMethod thirdDefencePositionCardMethodExecutor) {
        this.game = game;
        this.validator = validator;
        this.attackCardMethodExecutor = attackCardMethodExecutor;
        this.firstDefencePositionCardMethodExecutor = firstDefencePositionCardMethodExecutor;
        this.secondDefencePositionCardMethodExecutor = secondDefencePositionCardMethodExecutor;
        this.thirdDefencePositionCardMethodExecutor = thirdDefencePositionCardMethodExecutor;
    }

    /**
     * Returns next attack player.
     * @return Player next attack player.
     */
    public final Player getNextAttackPlayer() {
        final Card attack = game.getTrickCards().getAttackCard();

        Assert.assertNotNull(attack, Card.class);

        return getNextAttackPlayer(attack);
    }

    /**
     * Returns next attack player.
     * @param attack the trick attack one.
     * @return Player next attack player.
     */
    protected abstract Player getNextAttackPlayer(final Card attack);

    /**
     * Returns playing card for the provided player.
     * @param player provided player.
     * @return Card playing card for the provided player.
     */
    public final Card getPlayerCard(final Player player) {
        // attack player
        Player trickPlayer = game.getTrickAttackPlayer();
        if (player.equals(trickPlayer)) {
            return getAttackCard(player);
        }
        // first defense player
        trickPlayer = game.getPlayerAfter(trickPlayer);
        if (player.equals(trickPlayer)) {
            return getFirstDefencePositionCard(player);
        }
        // second defense player
        trickPlayer = game.getPlayerAfter(trickPlayer);
        if (player.equals(trickPlayer)) {
            return getSecondDefencePositionCard(player);
        }
        // third defense player
        return getThirdDefencePositionCard(player);
    }

    /**
     * Returns attack playing card for the provided player (AI).
     * @param player provided player.
     * @return Card attack playing card for the provided player (AI).
     */
    protected final Card getAttackCard(final Player player) {
        return attackCardMethodExecutor.getPlayerCard(player);
    }

    /**
     * Returns first defense position playing card for the provided player (AI).
     * @param player provided player.
     * @return Card first defense position playing card for the provided player (AI).
     */
    protected final Card getFirstDefencePositionCard(final Player player) {
        return firstDefencePositionCardMethodExecutor.getPlayerCard(player);
    }

    /**
     * Returns second defense position playing card for the provided player (AI).
     * @param player provided player.
     * @return Card second defense position playing card for the provided player (AI).
     */
    protected final Card getSecondDefencePositionCard(final Player player) {
        return secondDefencePositionCardMethodExecutor.getPlayerCard(player);
    }

    /**
     * Returns third defense position playing card for the provided player (AI).
     * @param player provided player.
     * @return Card third defense position playing card for the provided player (AI).
     */
    protected final Card getThirdDefencePositionCard(final Player player) {
        return thirdDefencePositionCardMethodExecutor.getPlayerCard(player);
    }

    /**
     * Validates player card.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if the card is valid, false otherwise.
     */
    public final boolean validatePlayerCard(final Player player, final Card card) {
        return validator.validatePlayerCard(player, card);
    }

    /**
     * Returns if the provided player has couple.
     * @param player provided player.
     * @param card provided card.
     * @return boolean true if has a couple false otherwise.
     */
    public final boolean hasPlayerCouple(final Player player, final Card card) {
        return validator.hasPlayerCouple(player, card);
    }
}
