package belote.logic.play.strategy.automat.methods.trumpsLess;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.pack.PackIterator;
import belote.bean.pack.card.Card;
import belote.logic.play.strategy.automat.base.method.BaseMethod;

public class MaximumOfAllMastersCard extends BaseMethod {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public MaximumOfAllMastersCard(final Game game) {
        super(game);
    }

    /**
     * Returns player's card.
     * @param player who is on turn.
     * @return Card object instance or null.
     */
    public Card getPlayMethodCard(final Player player) {
        Card result = null;
        
        final Card handAttackSuitCard = game.getTrickCards().getHandAttackSuitCard();
        if (handAttackSuitCard != null) {
            final Player partner = player.getPartner();
            final Player handPlayer = game.getPlayerByCard(handAttackSuitCard);
            if (handPlayer != null) {
                if (handPlayer.equals(partner) && isMaxSuitCardLeft(handAttackSuitCard, false)) {
        
                    if (isSecondDefencePosition() && isAllCardsMasters(player)) {
                        for (final PackIterator iterator = player.getCards().iterator(); iterator.hasNext();) {
                            final Card card = iterator.next();
                            if (player.getCards().getSuitCount(card.getSuit()) > 1) {
                                if (result == null || result.compareRankTo(card) < 0) {
                                    result = card;
                                }
                            }
                        }
            
                        if (result != null) {
                            player.getJackAceSuits().add(result.getSuit());
                        }
                    }
                }
            }
        }
        return result;
    }
}