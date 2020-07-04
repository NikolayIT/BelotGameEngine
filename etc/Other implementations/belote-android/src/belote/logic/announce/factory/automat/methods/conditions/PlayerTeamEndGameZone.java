/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.methods.conditions;

import belote.bean.Player;
import belote.logic.announce.factory.automat.methods.conditions.base.TeamEndGameZone;

/**
 * PlayerTeamEndGameZone class. Returns true if the announce player team is in the end game zone, false otherwise.
 * @author Dimitar Karamanov
 */
public final class PlayerTeamEndGameZone extends TeamEndGameZone{

    /**
     * The method which returns the result of condition.
     * @param player which has to declare next game announce.
     * @return boolean true if the condition fits, false otherwise.
     */
    public boolean process(final Player player) {
        final int playerTeamPoints = player.getTeam().getPoints().getAllPoints();
        return process(playerTeamPoints);
    }
}
