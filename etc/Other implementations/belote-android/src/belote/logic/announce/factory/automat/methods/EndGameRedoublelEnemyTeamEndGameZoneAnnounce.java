/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package belote.logic.announce.factory.automat.methods;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.base.AnnounceMethod;
import belote.logic.announce.factory.automat.methods.base.ConditionListMethod;
import belote.logic.announce.factory.automat.methods.conditions.OppositeTeamEndGameZone;

/**
 * @author Dimitar Karamanov
 */
public class EndGameRedoublelEnemyTeamEndGameZoneAnnounce extends ConditionListMethod {

    /**
     * Redouble announce factory helper.
     */
    private final AnnounceMethod redoubleAnnounce;

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public EndGameRedoublelEnemyTeamEndGameZoneAnnounce(final Game game) {
        super(game);
        redoubleAnnounce = new RedoubleAnnounce(game);
        addAnnounceCondition(new OppositeTeamEndGameZone(game));
    }

    /**
     * Returns the proper Announce when conditions match.
     * @param player who is on turn.
     * @return an Announce instance.
     */
    protected Announce createAnnounce(Player player) {
        return redoubleAnnounce.getAnnounce(player);
    }
}
