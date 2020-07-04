/*
 * Copyright (c) Dimitar Karamanov 2008-2014. All Rights Reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the source code must retain
 * the above copyright notice and the following disclaimer.
 *
 * This software is provided "AS IS," without a warranty of any kind.
 */
package belote.logic.announce.factory.transformers;

import belote.bean.Game;

/**
 * BaseAnnounceTransformer class.
 * @author Dimitar Karamanov
 */
public abstract class BaseAnnounceTransformer implements AnnounceTransformer {

    /**
     * BelotGame instance.
     */
    protected final Game game;

    /**
     * Constructor.
     * @param game BelotGame instance.
     */
    public BaseAnnounceTransformer(final Game game) {
        this.game = game;
    }
}
