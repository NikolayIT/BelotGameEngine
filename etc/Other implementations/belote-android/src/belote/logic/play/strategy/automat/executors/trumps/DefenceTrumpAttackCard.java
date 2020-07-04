/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.executors.trumps;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.bean.announce.AnnounceUnit;
import belote.bean.pack.card.Card;
import belote.bean.pack.card.suit.Suit;
import belote.logic.play.strategy.automat.base.executor.PlayCardExecutor;
import belote.logic.play.strategy.automat.methods.trumps.trumpAttack.MaxTrumpCard;
import belote.logic.play.strategy.automat.methods.trumps.trumpAttack.MinAboveCard;
import belote.logic.play.strategy.automat.methods.trumps.trumpAttack.MinTrumpCard;

/**
 * DefenceTrumpAttackCard executor. Implements the obligatory rules for defense player when the attack card is from trump suit. Used in TrumpDefenceCard executor.
 * @author Dimitar Karamanov
 */
public final class DefenceTrumpAttackCard extends PlayCardExecutor {
    
    /**
     * Handler method providing the user facility to check custom condition for methods executions.
     * @param player for which is called the executor
     * @return true to process method execution false to not.
     */
    protected boolean fitPreCondition(final Player player) {
        final Announce announce = game.getAnnounceList().getContractAnnounce();
        if (announce != null && announce.isTrumpAnnounce()) {
            final Suit trump = AnnounceUnit.transformFromAnnounceSuitToSuit(announce.getAnnounceSuit());
            final Card attackCard = game.getTrickCards().getAttackCard();
            return attackCard != null && trump != null && attackCard.getSuit().equals(trump);
        }
        return false;
    }

    /**
     * Constructor.
     * @param game a BelotGame instance.
     */
    public DefenceTrumpAttackCard(final Game game) {
        super(game);
        // Register play card methods.
        register(new MaxTrumpCard(game));
        register(new MinAboveCard(game));
        register(new MinTrumpCard(game));
    }
}
