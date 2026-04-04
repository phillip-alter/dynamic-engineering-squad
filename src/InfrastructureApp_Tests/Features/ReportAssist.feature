Feature: Report Assist Autocomplete
  As a user
  I want autocomplete suggestions while typing a report description
  So that I can complete my report faster and more accurately

  Scenario: Suggestions appear when enough text is typed
    Given I am on the Report Issue page
    When I type "pot" into the report description box
    Then autocomplete suggestions should be displayed

  Scenario: Suggestions do not appear when input is too short
    Given I am on the Report Issue page
    When I type "p" into the report description box
    Then autocomplete suggestions should not be displayed

  Scenario: Clicking a suggestion fills the description box
    Given I am on the Report Issue page
    When I type "pot" into the report description box
    And I click the first autocomplete suggestion
    Then the description box should contain the selected suggestion