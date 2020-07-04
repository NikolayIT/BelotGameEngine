/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.play.strategy.automat.methods.trumpsLess;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

/**
 * PartnerAttackMaxSuitCard class. PlayCardMethod which implements the logic of playing the biggest card or the min sequence before it when the attack is from
 * partner.
 * @author Dimitar Karamanov
 */
public final class PartnerAttackMaxSuitCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public PartnerAttackMaxSuitCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        Card result = null;
        final Player partner = player.getPartner();
        final Card attackCard = game.getTrickCards().getAttackCard();

        if (attackCard != null && isSecondDefencePosition()) { // partner is the
                                                               // attack player
            final Card maxCard = player.getCards().findMaxSuitCard(attackCard.getSuit());
            if (maxCard != null && hasPlayerAttack(partner)) {
                result = player.getCards().getMinSequenceCardBefore(maxCard);
                Card handTrickCard = game.getTrickCards().getHandAttackSuitCard();
                if (handTrickCard != null && result.compareTo(handTrickCard) < 0) {
                    result = null;
                }
            }
        }
        return result;
    }
}