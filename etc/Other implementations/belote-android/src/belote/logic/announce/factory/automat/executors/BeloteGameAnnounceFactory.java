/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.automat.executors;

import belote.bean.Game;
import belote.bean.Player;
import belote.bean.announce.Announce;
import belote.logic.announce.factory.automat.executors.base.AnnounceExecutor;
import belote.logic.announce.factory.automat.methods.EndGameNagDoubleAnnounce;
import belote.logic.announce.factory.automat.methods.EndGamePassAnnounce;
import belote.logic.announce.factory.automat.methods.EndGameTrumpToAllTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.RegGameTrumpToAllTrumpAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.PartnerDoubleRedoubleContractAnnounce;
import belote.logic.announce.factory.automat.methods.conditions.base.NotCondition;

/**
 * BelotGameAnnounce class.
 * @author Dimitar Karamanov
 */
public final class BeloteGameAnnounceFactory extends AnnounceExecutor {

    /**
     * Constructor.
     * @param game BelotGame instance class.
     */
    public BeloteGameAnnounceFactory(final Game game) {
        super(game);
        
        addPreCondition(new NotCondition(new PartnerDoubleRedoubleContractAnnounce(game)));

        register(new EndGameOpenAnnounce(game));
        register(new EndGameNormalAnnounce(game));
        register(new EndGameSupportAnnounce(game));
        register(new EndGameTrumpToAllTrumpAnnounce(game));
        register(new EndGameNagDoubleAnnounce(game));
        
        register(new EndGamePassAnnounce(game));
        
        register(new RegGameOpenAnnounce(game));
        register(new RegGameNormalAnnounce(game));
        register(new RegGameSupportAnnounce(game));
        register(new RegGameTrumpToAllTrumpAnnounce(game));
        register(new RegGameNagAnnounce(game));
    }

    /**
     * Handler method providing the user to write additional code which is executed after the getAnnounce(Player).
     * @param player for which is called the executor
     * @param result the result of the method getAnnounce(Player)
     * @return Announce - the same or transformed one.
     */
    protected Announce afterExecution(final Player player, final Announce result) {
        if (result == null) {
            return Announce.createPassAnnounce(player);
        }
        return result;
    }
}
