﻿using System.Collections.Generic;
using EnjoyCQRS.Bus;
using EnjoyCQRS.Events;
using EnjoyCQRS.EventStore.Storage;
using EnjoyCQRS.UnitTests.Domain;
using FluentAssertions;
using Moq;
using Xunit;
using IUnitOfWork = EnjoyCQRS.EventStore.IUnitOfWork;

namespace EnjoyCQRS.UnitTests.Storage
{
    public class EventStoreTests
    {
        private readonly StubEventStore _inMemoryDomainEventStore = new StubEventStore();
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRepository _repository;
        private readonly Mock<IEventPublisher> _mockEventPublisher;
        private readonly IAggregateTracker _aggregateTracker;

        public EventStoreTests()
        {
            _mockEventPublisher = new Mock<IEventPublisher>();
            _mockEventPublisher.Setup(e => e.Publish(It.IsAny<IEnumerable<IDomainEvent>>()));

            _aggregateTracker = new AggregateTracker();
            
            var session = new Session(_aggregateTracker, _inMemoryDomainEventStore, _mockEventPublisher.Object);
            _repository = new Repository(session, _aggregateTracker);

            _unitOfWork = session;
        }

        [Fact]
        public void When_calling_Save_it_will_add_the_domain_events_to_the_domain_event_storage()
        {
            var testAggregate = TestAggregateRoot.Create();
            testAggregate.DoSomething("Heisenberg");

            _repository.Add(testAggregate);
            _unitOfWork.Commit();

            _inMemoryDomainEventStore.EventStore[testAggregate.Id].Count.Should().Be(2);
        }

        [Fact]
        public void When_calling_Save_the_uncommited_events_should_be_published()
        {
            var testAggregate = TestAggregateRoot.Create();
            testAggregate.DoSomething("Heisenberg");

            _repository.Add(testAggregate);
            _unitOfWork.Commit();

            _mockEventPublisher.Verify(e => e.Publish(It.IsAny<IEnumerable<IDomainEvent>>()));
        }

        [Fact]
        public void When_load_aggregate_should_be_correct_version()
        {
            var testAggregate = TestAggregateRoot.Create();
            testAggregate.DoSomething("Heisenberg");

            _repository.Add(testAggregate);
            _unitOfWork.Commit();

            var testAggregate2 = _repository.GetById<TestAggregateRoot>(testAggregate.Id);
            
            testAggregate.Version.Should().Be(testAggregate2.Version);
        }
    }
}